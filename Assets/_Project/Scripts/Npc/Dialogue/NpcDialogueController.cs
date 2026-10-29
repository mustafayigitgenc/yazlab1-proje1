using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Gumusdere.Core;
using Gumusdere.Items;
using Gumusdere.Npc.Actions;
using Gumusdere.Npc.Llm;
using Gumusdere.Npc.Parsing;
using UnityEngine;

namespace Gumusdere.Npc.Dialogue
{
    /// <summary>
    /// Hüsnü Dayı ile sohbetin akışını yönetir:
    ///   oyuncu yazar → istem kurulur → yerel LLM'e asenkron istek → JSON ayrıştırılır →
    ///   güven güncellenir → hediye ve eylem oyun kurallarıyla kontrol edilip uygulanır.
    /// Hiçbir hata oyunu durdurmaz; her hata "[Sistem]" satırı olarak oyuncuya anlatılır.
    /// Bu sınıf ağ, ayrıştırma veya çizim ayrıntısını bilmez; hepsini arayüzler üzerinden kullanır.
    /// </summary>
    public sealed class NpcDialogueController : MonoBehaviour
    {
        private const int MaxPlayerMessageLength = 300;
        private const float HealthCheckInterval = 10f;
        private const int MaxLines = 40;

        private readonly List<DialogueLine> lines = new();
        private readonly List<ItemType> giftsReceived = new();
        private readonly Dictionary<NpcAction, IGameAction> actions = new();

        private ILLMClient client;
        private IResponseParser<NpcDecision> parser;
        private NpcPersona persona;
        private PromptBuilder promptBuilder;
        private ConversationHistory history;
        private IInventory inventory;
        private ISpeaker npcSpeaker;
        private Action<bool> setPlayerControls;
        private CancellationTokenSource lifetime;
        private bool warmedUp;

        public NpcPersona Persona => persona;
        public TrustMeter Trust { get; private set; }
        public IReadOnlyList<DialogueLine> Lines => lines;
        public bool IsOpen { get; private set; }
        public bool IsThinking { get; private set; }
        public float ThinkingSince { get; private set; }
        public NpcEmotion LastEmotion { get; private set; } = NpcEmotion.Neutral;
        public LlmHealth Health { get; private set; } = LlmHealth.Unknown;
        public string ModelName => client?.ModelName ?? "-";

        /// <summary>Başarılı her karar (karar, gecikme ms). Testler ve loglar için.</summary>
        public event Action<NpcDecision, long> DecisionReceived;

        public void Initialize(ILLMClient llmClient, IResponseParser<NpcDecision> responseParser, NpcPersona npcPersona,
            IInventory playerInventory, ISpeaker speaker, Action<bool> controlsSetter)
        {
            client = llmClient;
            parser = responseParser;
            persona = npcPersona;
            inventory = playerInventory;
            npcSpeaker = speaker;
            setPlayerControls = controlsSetter;

            promptBuilder = new PromptBuilder(persona);
            history = new ConversationHistory();
            Trust = new TrustMeter(persona.StartTrust);
            lifetime = new CancellationTokenSource();

            _ = HealthLoopAsync(lifetime.Token);
        }

        public void RegisterAction(NpcAction action, IGameAction handler) => actions[action] = handler;

        // ------------------------------------------------------------------ Pencere

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            setPlayerControls?.Invoke(false);

            if (lines.Count == 0)
            {
                AddLine(DialogueSpeaker.System, $"{persona.Name} maden kapısının önünde oturmuş, sana bakıyor. Ne söylemek istersin?");
            }

            if (Health == LlmHealth.Offline) AddLine(DialogueSpeaker.System, OfflineHint());
            else if (Health == LlmHealth.ModelMissing) AddLine(DialogueSpeaker.System, ModelMissingHint());
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            setPlayerControls?.Invoke(true);
        }

        // ------------------------------------------------------------------ Mesaj gönderme

        /// <summary>Arayüzden çağrılır. Beklemez; cevap gelince satırlar kendiliğinden eklenir.</summary>
        public void Submit(string text)
        {
            _ = SubmitAsync(text);
        }

        public async Task SubmitAsync(string text)
        {
            if (IsThinking || client == null) return;

            text = text?.Trim();
            if (string.IsNullOrEmpty(text)) return;
            if (text.Length > MaxPlayerMessageLength) text = text.Substring(0, MaxPlayerMessageLength);

            AddLine(DialogueSpeaker.Player, text);
            IsThinking = true;
            ThinkingSince = Time.time;

            try
            {
                await ProcessAsync(text, lifetime.Token);
            }
            catch (OperationCanceledException)
            {
                // Sahne kapanıyor.
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                AddLine(DialogueSpeaker.System, "Beklenmeyen bir hata oldu ama oyun devam ediyor. Tekrar yazmayı dene.");
            }
            finally
            {
                IsThinking = false;
            }
        }

        private async Task ProcessAsync(string playerText, CancellationToken token)
        {
            List<ChatMessage> messages = promptBuilder.Build(history, playerText, Trust.Value, inventory.Snapshot(), giftsReceived);

            LlmResult result = await ChatWithRetryAsync(messages, token);
            if (this == null || token.IsCancellationRequested) return;
            if (!result.Success)
            {
                AddLine(DialogueSpeaker.System, DescribeError(result));
                return;
            }

            if (!parser.TryParse(result.Content, out NpcDecision decision, out string parseError))
            {
                Debug.LogWarning($"[LLM] Geçersiz çıktı ({parseError}): {result.Content}");

                // Bir kez onarım isteği: modelden sadece geçerli JSON istenir.
                messages.Add(ChatMessage.FromAssistant(result.Content));
                messages.Add(ChatMessage.FromUser("Önceki cevabın geçerli JSON değildi. Aynı cevabı SADECE istenen JSON biçiminde tekrar ver."));
                LlmResult repaired = await client.ChatAsync(messages, NpcResponseParser.JsonSchema, token);
                if (this == null || token.IsCancellationRequested) return;

                if (!repaired.Success || !parser.TryParse(repaired.Content, out decision, out parseError))
                {
                    AddLine(DialogueSpeaker.System, $"{persona.Name}'nın cevabı anlaşılamadı (geçersiz JSON). Oyun durumu değişmedi; tekrar yazmayı dene.");
                    return;
                }
                result = repaired;
            }

            Apply(playerText, decision);
            DecisionReceived?.Invoke(decision, result.LatencyMs);
        }

        private async Task<LlmResult> ChatWithRetryAsync(List<ChatMessage> messages, CancellationToken token)
        {
            LlmResult result = await client.ChatAsync(messages, NpcResponseParser.JsonSchema, token);
            int retries = 1;
            while (!result.Success && result.Error == LlmErrorKind.Timeout && retries-- > 0 && !token.IsCancellationRequested)
            {
                AddLine(DialogueSpeaker.System, "Cevap gecikti, bir kez daha deneniyor...");
                result = await client.ChatAsync(messages, NpcResponseParser.JsonSchema, token);
            }

            if (result.Success) Health = LlmHealth.Online;
            else if (result.Error == LlmErrorKind.ServerUnreachable) Health = LlmHealth.Offline;
            else if (result.Error == LlmErrorKind.ModelNotFound) Health = LlmHealth.ModelMissing;
            return result;
        }

        private void Apply(string playerText, NpcDecision decision)
        {
            history.AddTurn(playerText, decision.RawJson);
            foreach (string warning in decision.Warnings) Debug.Log($"[LLM] Düzeltme: {warning}");

            LastEmotion = decision.Emotion;
            Trust.Apply(decision.TrustChange);
            AddLine(DialogueSpeaker.Npc, decision.Reply);
            npcSpeaker?.Say(decision.Reply, 4f);

            if (decision.AcceptedGift.HasValue) TryAcceptGift(decision.AcceptedGift.Value);

            Debug.Log($"[LLM] trust {decision.TrustChange:+0;-0;0} → {Trust.Value}, emotion {decision.Emotion}, action {decision.Action}");
            if (decision.Action != NpcAction.None) TryRunAction(decision.Action);
        }

        private void TryAcceptGift(ItemType gift)
        {
            // Model "aldım" dese bile eşya gerçekten çantada yoksa hiçbir şey olmaz.
            if (!inventory.TryRemove(gift))
            {
                Debug.Log($"[Kural] Hediye reddedildi: çantada {ItemInfo.DisplayName(gift)} yok.");
                return;
            }

            giftsReceived.Add(gift);
            AddLine(DialogueSpeaker.System, $"{persona.Name}'ya 1 {ItemInfo.DisplayName(gift)} verdin.");
        }

        private void TryRunAction(NpcAction action)
        {
            if (!actions.TryGetValue(action, out IGameAction handler))
            {
                Debug.LogWarning($"[Kural] '{action}' için tanımlı eylem yok.");
                return;
            }

            if (!handler.CanExecute(Trust.Value, out string reason))
            {
                Debug.Log($"[Kural] Model '{handler.Name}' istedi ama oyun kuralı reddetti: {reason}");
                return;
            }

            Debug.Log($"[Kural] Eylem uygulanıyor: {handler.Name}");
            handler.Execute();
        }

        // ------------------------------------------------------------------ Sunucu sağlık kontrolü

        private async Task HealthLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested && this != null)
            {
                if (!IsThinking)
                {
                    LlmHealth previous = Health;
                    Health = await client.CheckHealthAsync(token);
                    if (this == null || token.IsCancellationRequested) return;

                    if (Health != previous) Debug.Log($"[Ollama] Durum: {Health}");
                    if (Health == LlmHealth.Online && !warmedUp)
                    {
                        warmedUp = true;
                        _ = client.WarmUpAsync(token);
                    }
                }

                float until = Time.realtimeSinceStartup + HealthCheckInterval;
                while (Time.realtimeSinceStartup < until)
                {
                    if (token.IsCancellationRequested || this == null) return;
                    await Task.Yield();
                }
            }
        }

        // ------------------------------------------------------------------ Yardımcılar

        private void AddLine(DialogueSpeaker speaker, string text)
        {
            lines.Add(new DialogueLine(speaker, text));
            if (lines.Count > MaxLines) lines.RemoveAt(0);
        }

        private string DescribeError(LlmResult result) => result.Error switch
        {
            LlmErrorKind.ServerUnreachable => OfflineHint(),
            LlmErrorKind.ModelNotFound => ModelMissingHint(),
            LlmErrorKind.Timeout => $"{persona.Name} çok uzun süre sessiz kaldı (zaman aşımı). Model hâlâ yükleniyor olabilir; biraz sonra tekrar dene.",
            LlmErrorKind.Cancelled => "İstek iptal edildi.",
            _ => $"Dil modelinden geçerli bir cevap alınamadı: {result.ErrorMessage}"
        };

        private static string OfflineHint() =>
            "Yerel dil modeli sunucusuna (Ollama) ulaşılamıyor. Ollama'yı başlatıp tekrar yaz; oyun bu arada devam ediyor.";

        private string ModelMissingHint() =>
            $"Ollama çalışıyor ama '{client.ModelName}' modeli yüklü değil. Terminalde: ollama pull {client.ModelName}";

        private void OnDestroy()
        {
            lifetime?.Cancel();
            lifetime?.Dispose();
            lifetime = null;
        }
    }
}
