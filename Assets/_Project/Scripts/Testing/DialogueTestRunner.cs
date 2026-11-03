using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Gumusdere.Items;
using Gumusdere.Mining;
using Gumusdere.Npc.Dialogue;
using Gumusdere.Npc.Llm;
using Gumusdere.Npc.Parsing;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gumusdere.Testing
{
    /// <summary>
    /// Diyalog testi (F10). Her senaryo için oyundakiyle AYNI NpcDialogueController, istem kurucu,
    /// JSON ayrıştırıcı ve oyun kuralları kullanılır; sadece kapı/bekçi yerine kayıt tutan sahte eylemler takılır
    /// ve oyuncunun çantası senaryoya göre hazırlanır. Oyundaki Hüsnü Dayı'nın güveni ve sohbeti etkilenmez.
    /// Sonuç: TestResults/dialogue_test_*.csv ve dialogue_test_*.md
    /// </summary>
    public sealed class DialogueTestRunner : MonoBehaviour
    {
        private const float ScenarioTimeout = 150f; // gerçek saniye; denetleyicinin kendi zaman aşımları bunun içinde kalır

        private readonly List<(DialogueScenario scenario, int run, DialogueObservation obs, bool pass)> results = new();
        private Func<bool> canStart;
        private bool cancelRequested;
        private string status;
        private float statusUntil;
        private int exceptionCount;

        public bool IsRunning { get; private set; }

        public void Initialize(Func<bool> startAllowed)
        {
            canStart = startAllowed;
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null || !kb.f10Key.wasPressedThisFrame) return;

            if (IsRunning)
            {
                // Yanlışlıkla ikinci kez basınca test bozulmasın: iptal sadece Shift+F10 ile.
                if (kb.shiftKey.isPressed)
                {
                    cancelRequested = true;
                    status = "İptal ediliyor (mevcut istek bitince)...";
                }
                return;
            }

            if (canStart != null && !canStart())
            {
                ShowStatus("Diyalog testi şu an başlatılamaz (oyun oynanıyor olmalı, sohbet penceresi kapalı olmalı).", 4f);
                return;
            }

            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            IsRunning = true;
            cancelRequested = false;
            results.Clear();
            Application.logMessageReceived += OnLog;

            List<DialogueScenario> scenarios = DialogueScenarios.All();
            OllamaConfig config = OllamaConfig.LoadOrDefault();

            // Gerçek model açık mı? Kapalıysa D senaryoları atlanır, H senaryoları yine çalışır.
            status = $"Ollama kontrol ediliyor ({config.model})...";
            Task<LlmHealth> healthTask = new OllamaClient(config).CheckHealthAsync(CancellationToken.None);
            while (!healthTask.IsCompleted) yield return null;
            bool modelOnline = healthTask.Status == TaskStatus.RanToCompletion && healthTask.Result == LlmHealth.Online;
            Debug.Log($"[DiyalogTesti] Başladı. Model: {config.model}, durum: {(healthTask.IsCompletedSuccessfully ? healthTask.Result.ToString() : "?")}");

            int total = scenarios.Sum(s => s.UsesRealModel && !modelOnline ? 0 : s.Runs);
            int done = 0;

            foreach (DialogueScenario scenario in scenarios)
            {
                if (cancelRequested) break;
                if (scenario.UsesRealModel && !modelOnline) continue;

                for (int run = 1; run <= scenario.Runs && !cancelRequested; run++)
                {
                    status = $"{scenario.Id} {scenario.Title}  (tekrar {run}/{scenario.Runs})\n" +
                             $"İlerleme: {done}/{total}  •  Geçen: {results.Count(r => r.pass)}/{results.Count}\n" +
                             (scenario.UsesRealModel ? $"Model: {config.model} — Hüsnü Dayı düşünüyor..." : "Sahte istemci (hata senaryosu)") +
                             "\n(Shift+F10: iptal)";

                    var obs = new DialogueObservation();
                    yield return RunOnce(scenario, config, obs);

                    bool pass;
                    try { pass = scenario.Pass(obs); }
                    catch (Exception e) { pass = false; obs.Exception ??= "Değerlendirme hatası: " + e.Message; }

                    results.Add((scenario, run, obs, pass));
                    done++;
                    Debug.Log($"[DiyalogTesti] {scenario.Id}#{run} {(pass ? "GEÇTİ" : "KALDI")} — eylem {obs.LlmAction}, " +
                              $"güven {obs.TrustBefore}→{obs.TrustAfter}, {obs.LatencyMs} ms, cevap: {obs.Decision?.Reply ?? obs.LastSystemLine}");
                }
            }

            Application.logMessageReceived -= OnLog;
            IsRunning = false;
            string summary = WriteReports(config, modelOnline);
            ShowStatus(summary, 25f);
        }

        /// <summary>Tek bir senaryo tekrarı: yeni bir denetleyici kur, oyuncunun sözünü gönder, sonucu gözle.</summary>
        private IEnumerator RunOnce(DialogueScenario scenario, OllamaConfig config, DialogueObservation obs)
        {
            NpcPersona persona = NpcPersona.HusnuDayi();
            var inventory = new Inventory();
            foreach (ItemType item in scenario.Inventory) inventory.Add(item);

            bool mineOpen = scenario.MineOpen;
            // Kapıyı açma izni oyundaki ile aynı kuraldan gelir (güven eşiği + baret ve fener).
            var open = new RecordingAction("open_mine", t => MineEntryRule.Check(t, persona.OpenThreshold, mineOpen, inventory));
            var close = new RecordingAction("close_mine", _ => mineOpen ? null : "Kapı zaten kapalı");
            var kick = new RecordingAction("kick_out", t => t >= persona.KickThreshold ? $"Güven hâlâ yüksek ({t})" : null);

            var client = new CountingLlmClient(scenario.ClientFactory != null ? scenario.ClientFactory() : new OllamaClient(config));

            var go = new GameObject($"DiyalogTesti_{scenario.Id}");
            go.transform.SetParent(transform, false);
            var controller = go.AddComponent<NpcDialogueController>();
            controller.Initialize(client, new NpcResponseParser(), persona, inventory, null, null);
            controller.RegisterAction(NpcAction.OpenMine, open);
            controller.RegisterAction(NpcAction.CloseMine, close);
            controller.RegisterAction(NpcAction.KickOut, kick);
            controller.SetMineStateSource(() => mineOpen);
            controller.Trust.Reset(scenario.StartTrust);
            controller.DecisionReceived += (decision, ms) =>
            {
                obs.Decision = decision;
                obs.LatencyMs = ms;
            };

            obs.TrustBefore = controller.Trust.Value;
            obs.InventoryBefore = inventory.Snapshot().Values.Sum();
            int linesBefore = controller.Lines.Count;
            int exceptionsBefore = exceptionCount;
            float startedAt = Time.realtimeSinceStartup;

            Task task = controller.SubmitAsync(scenario.PlayerText);
            while (!task.IsCompleted && Time.realtimeSinceStartup - startedAt < ScenarioTimeout) yield return null;
            if (!task.IsCompleted) obs.Exception = $"{ScenarioTimeout:0} sn içinde bitmedi";
            else if (task.IsFaulted) obs.Exception = task.Exception?.GetBaseException().Message;

            obs.WallSeconds = Time.realtimeSinceStartup - startedAt;
            obs.TrustAfter = controller.Trust.Value;
            obs.InventoryAfter = inventory.Snapshot().Values.Sum();
            obs.Opened = open.Executed;
            obs.Closed = close.Executed;
            obs.Kicked = kick.Executed;
            obs.LlmCalls = client.Calls;
            obs.GiftEffect = obs.Kicked ? 0 : obs.TrustAfter - obs.TrustBefore - obs.LlmTrustChange;
            if (exceptionCount > exceptionsBefore && obs.Exception == null) obs.Exception = "Konsola istisna (exception) düştü";

            for (int i = controller.Lines.Count - 1; i >= 0 && i >= linesBefore - 1; i--)
            {
                if (controller.Lines[i].Speaker != DialogueSpeaker.System) continue;
                obs.LastSystemLine = controller.Lines[i].Text;
                break;
            }

            Destroy(go); // denetleyicinin sağlık döngüsü OnDestroy'da iptal edilir
            yield return null;
        }

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Exception) exceptionCount++;
        }

        // ------------------------------------------------------------------ Rapor

        private string WriteReports(OllamaConfig config, bool modelOnline)
        {
            string stamp = TestReport.Stamp();

            string[] header =
            {
                "senaryo", "baslik", "tekrar", "gecti", "beklenen", "oyuncu", "json_gecerli", "istek_sayisi", "gecikme_ms",
                "guven_once", "llm_trust_change", "hediye_etkisi", "guven_sonra", "llm_eylem", "kapi_acildi", "kapi_kapandi",
                "kovuldu", "llm_hediye", "hediye_alindi", "duygu", "uyarilar", "cevap", "sistem_mesaji", "hata"
            };
            var rows = results.Select(r => (IReadOnlyList<string>)new[]
            {
                r.scenario.Id, r.scenario.Title, r.run.ToString(), r.pass ? "evet" : "hayir", r.scenario.Expectation, r.scenario.PlayerText,
                r.obs.GotDecision ? "evet" : "hayir", r.obs.LlmCalls.ToString(), r.obs.LatencyMs.ToString(),
                r.obs.TrustBefore.ToString(), r.obs.LlmTrustChange.ToString(), r.obs.GiftEffect.ToString(), r.obs.TrustAfter.ToString(),
                ActionKey(r.obs.LlmAction), Yes(r.obs.Opened), Yes(r.obs.Closed), Yes(r.obs.Kicked),
                r.obs.Decision?.AcceptedGift is ItemType g ? ItemInfo.Key(g) : "none", Yes(r.obs.GiftAccepted),
                r.obs.Decision?.Emotion.ToString().ToLowerInvariant() ?? "", r.obs.Decision != null ? string.Join(" / ", r.obs.Decision.Warnings) : "",
                r.obs.Decision?.Reply ?? "", r.obs.LastSystemLine ?? "", r.obs.Exception ?? ""
            });
            string csv = TestReport.WriteCsv($"dialogue_test_{stamp}.csv", header, rows);

            var md = new StringBuilder();
            md.AppendLine($"### Diyalog testi ({DateTime.Now:dd.MM.yyyy HH:mm})");
            md.AppendLine();
            md.AppendLine($"Model: `{config.model}` (temperature {TestReport.F(config.temperature, 1)}, zaman aşımı {config.timeoutSeconds} sn). " +
                          (modelOnline ? "Gerçek model senaryoları 3'er kez çalıştırıldı." : "**Ollama kapalıydı; gerçek model senaryoları atlandı.**"));
            md.AppendLine();

            var model = results.Where(r => r.scenario.UsesRealModel).ToList();
            if (model.Count > 0)
            {
                int valid = model.Count(r => r.obs.GotDecision);
                int firstTry = model.Count(r => r.obs.GotDecision && r.obs.LlmCalls == 1);
                var latencies = model.Where(r => r.obs.GotDecision).Select(r => (float)r.obs.LatencyMs).ToList();
                md.AppendLine($"Geçerli JSON: {valid}/{model.Count} ({TestReport.Percent(valid, model.Count)}), " +
                              $"ilk denemede geçerli: {firstTry}/{model.Count}, " +
                              $"ortalama gecikme: {(latencies.Count > 0 ? latencies.Average() : 0f):0} ms " +
                              $"(en az {(latencies.Count > 0 ? latencies.Min() : 0f):0}, en çok {(latencies.Count > 0 ? latencies.Max() : 0f):0}).");
                md.AppendLine();
                md.AppendLine("| # | Senaryo | Oyuncunun sözü | Beklenen | Geçen | Modelin eylemi (3 tekrar) | Güven değişimi | Ort. gecikme (ms) | Örnek cevap |");
                md.AppendLine("|---|---|---|---|---|---|---|---|---|");
                foreach (var group in model.GroupBy(r => r.scenario.Id))
                {
                    var list = group.ToList();
                    DialogueScenario s = list[0].scenario;
                    string actions = string.Join(", ", list.Select(r => r.obs.GotDecision ? ActionKey(r.obs.LlmAction) : "hata"));
                    string trust = string.Join(", ", list.Select(r => $"{r.obs.TrustBefore}→{r.obs.TrustAfter}"));
                    var ok = list.Where(r => r.obs.GotDecision).ToList();
                    string latency = ok.Count > 0 ? ok.Average(r => (float)r.obs.LatencyMs).ToString("F0") : "-";
                    string sample = list.Select(r => r.obs.Decision?.Reply).FirstOrDefault(t => !string.IsNullOrEmpty(t)) ?? "-";
                    md.AppendLine($"| {s.Id} | {TestReport.Md(s.Title)} | \"{TestReport.Md(s.PlayerText)}\" | {TestReport.Md(s.Expectation)} | " +
                                  $"{list.Count(r => r.pass)}/{list.Count} | {actions} | {trust} | {latency} | {TestReport.Md(sample)} |");
                }
                md.AppendLine();
            }

            var robust = results.Where(r => !r.scenario.UsesRealModel).ToList();
            if (robust.Count > 0)
            {
                md.AppendLine("| # | Hata / kural senaryosu | Beklenen | Sonuç | Oyunun verdiği tepki |");
                md.AppendLine("|---|---|---|---|---|");
                foreach (var r in robust)
                {
                    string reaction = r.obs.GotDecision
                        ? $"Karar: {ActionKey(r.obs.LlmAction)}, güven {r.obs.TrustBefore}→{r.obs.TrustAfter}" +
                          (r.obs.Opened ? ", kapı açıldı" : "") + (r.obs.Kicked ? ", kovuldu" : "") +
                          (r.obs.Decision.Warnings.Count > 0 ? $", düzeltme: {string.Join("; ", r.obs.Decision.Warnings)}" : "")
                        : r.obs.LastSystemLine ?? "-";
                    if (r.obs.Exception != null) reaction += $" (HATA: {r.obs.Exception})";
                    md.AppendLine($"| {r.scenario.Id} | {TestReport.Md(r.scenario.Title)} | {TestReport.Md(r.scenario.Expectation)} | " +
                                  $"{(r.pass ? "✅ geçti" : "❌ kaldı")} | {TestReport.Md(reaction)} |");
                }
            }
            string mdPath = TestReport.WriteText($"dialogue_test_{stamp}.md", md.ToString());

            int passed = results.Count(r => r.pass);
            return $"Bitti: {passed}/{results.Count} senaryo tekrarı geçti" +
                   (modelOnline ? "" : " (Ollama kapalı: sadece hata senaryoları)") + "\n" +
                   $"Dosyalar: {(csv != null ? System.IO.Path.GetFileName(csv) : "yazılamadı")}, " +
                   $"{(mdPath != null ? System.IO.Path.GetFileName(mdPath) : "yazılamadı")}\n" +
                   $"Klasör: {TestReport.ResultsFolder}";
        }

        private static string ActionKey(NpcAction action) => action switch
        {
            NpcAction.OpenMine => "open_mine",
            NpcAction.CloseMine => "close_mine",
            NpcAction.KickOut => "kick_out",
            _ => "none"
        };

        private static string Yes(bool value) => value ? "evet" : "hayir";

        // ------------------------------------------------------------------ Ekran

        private void ShowStatus(string text, float seconds)
        {
            status = text;
            statusUntil = Time.unscaledTime + seconds;
        }

        private void OnGUI()
        {
            if (!IsRunning && Time.unscaledTime > statusUntil) return;
            TestPanel.Draw("Diyalog testi (F10)", status);
        }
    }
}
