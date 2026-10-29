namespace Gumusdere.Npc.Llm
{
    /// <summary>LLM isteğinin neden başarısız olduğu.</summary>
    public enum LlmErrorKind
    {
        None,
        ServerUnreachable,
        ModelNotFound,
        Timeout,
        HttpError,
        InvalidResponse,
        Cancelled
    }

    /// <summary>
    /// Bir LLM isteğinin sonucu. İstisna (exception) fırlatmak yerine sonuç döndürülür;
    /// böylece çağıran taraf her hata türünü ayrı ayrı ve güvenle ele alabilir.
    /// </summary>
    public sealed class LlmResult
    {
        private LlmResult(bool success, string content, LlmErrorKind error, string message, long latencyMs)
        {
            Success = success;
            Content = content;
            Error = error;
            ErrorMessage = message;
            LatencyMs = latencyMs;
        }

        public bool Success { get; }
        public string Content { get; }
        public LlmErrorKind Error { get; }
        public string ErrorMessage { get; }
        public long LatencyMs { get; }

        public static LlmResult Ok(string content, long latencyMs) =>
            new(true, content, LlmErrorKind.None, null, latencyMs);

        public static LlmResult Fail(LlmErrorKind error, string message, long latencyMs = 0) =>
            new(false, null, error, message, latencyMs);
    }

    /// <summary>LLM sunucusunun durumu.</summary>
    public enum LlmHealth
    {
        Unknown,
        Online,
        ModelMissing,
        Offline
    }
}
