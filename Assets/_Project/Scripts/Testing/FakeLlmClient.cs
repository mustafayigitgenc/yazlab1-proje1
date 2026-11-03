using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Gumusdere.Npc.Llm;

namespace Gumusdere.Testing
{
    /// <summary>
    /// Sırayla önceden yazılmış cevaplar döndüren sahte LLM istemcisi.
    /// Zaman aşımı, bozuk JSON, şema dışı değer gibi hata durumlarını gerçek modele bağlı kalmadan,
    /// her seferinde aynı şekilde test etmek için kullanılır (ILLMClient arayüzü sayesinde
    /// NpcDialogueController hiçbir değişiklik olmadan bu istemciyle çalışır).
    /// </summary>
    public sealed class FakeLlmClient : ILLMClient
    {
        private readonly Queue<LlmResult> script;

        public FakeLlmClient(params LlmResult[] results)
        {
            script = new Queue<LlmResult>(results);
        }

        public string ModelName => "sahte-model";
        public int Calls { get; private set; }

        public async Task<LlmResult> ChatAsync(IReadOnlyList<ChatMessage> messages, string jsonSchema, CancellationToken cancellation)
        {
            Calls++;
            await Task.Yield(); // gerçek istek gibi en az bir kare sürsün
            return script.Count > 0
                ? script.Dequeue()
                : LlmResult.Fail(LlmErrorKind.InvalidResponse, "Sahte istemcide cevap kalmadı.");
        }

        public Task<LlmHealth> CheckHealthAsync(CancellationToken cancellation) => Task.FromResult(LlmHealth.Online);

        public Task WarmUpAsync(CancellationToken cancellation) => Task.CompletedTask;
    }
}
