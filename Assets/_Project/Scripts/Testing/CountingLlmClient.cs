using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Gumusdere.Npc.Llm;

namespace Gumusdere.Testing
{
    /// <summary>
    /// Başka bir istemciyi saran ve kaç sohbet isteği yapıldığını sayan dekoratör.
    /// 1'den fazla istek = zaman aşımı tekrarı ya da bozuk JSON için onarım isteği yapılmış demektir.
    /// </summary>
    public sealed class CountingLlmClient : ILLMClient
    {
        private readonly ILLMClient inner;

        public CountingLlmClient(ILLMClient inner)
        {
            this.inner = inner;
        }

        public int Calls { get; private set; }
        public string ModelName => inner.ModelName;

        public Task<LlmResult> ChatAsync(IReadOnlyList<ChatMessage> messages, string jsonSchema, CancellationToken cancellation)
        {
            Calls++;
            return inner.ChatAsync(messages, jsonSchema, cancellation);
        }

        public Task<LlmHealth> CheckHealthAsync(CancellationToken cancellation) => inner.CheckHealthAsync(cancellation);

        public Task WarmUpAsync(CancellationToken cancellation) => inner.WarmUpAsync(cancellation);
    }
}
