using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Gumusdere.Npc.Llm
{
    /// <summary>
    /// Yerel dil modeli istemcisi sözleşmesi. NPC tarafı sadece bu arayüzü bilir;
    /// Ollama yerine başka bir yerel sunucu (LM Studio vb.) takılabilir (Dependency Inversion).
    /// </summary>
    public interface ILLMClient
    {
        string ModelName { get; }

        /// <summary>Mesajları gönderir; jsonSchema verilirse model çıktısı bu şemaya zorlanır.</summary>
        Task<LlmResult> ChatAsync(IReadOnlyList<ChatMessage> messages, string jsonSchema, CancellationToken cancellation);

        /// <summary>Sunucu açık mı ve model yüklü mü.</summary>
        Task<LlmHealth> CheckHealthAsync(CancellationToken cancellation);

        /// <summary>Modeli belleğe önceden yükler; ilk cevap daha hızlı gelir.</summary>
        Task WarmUpAsync(CancellationToken cancellation);
    }
}
