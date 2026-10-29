using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

namespace Gumusdere.Npc.Llm
{
    /// <summary>
    /// Ollama'nın yerel REST API'si ile konuşan istemci (http://localhost:11434).
    ///   POST /api/chat     : sohbet, "format" alanına JSON şeması verilir (yapılandırılmış çıktı)
    ///   GET  /api/tags     : sunucu açık mı, model yüklü mü
    ///   POST /api/generate : boş istekle modeli belleğe önceden yükleme
    /// İstekler UnityWebRequest ile asenkron gider; ana döngü (FPS) hiç beklemez.
    /// Hiçbir hata dışarı istisna olarak sızmaz, her durum LlmResult olarak döner.
    /// </summary>
    public sealed class OllamaClient : ILLMClient
    {
        private readonly OllamaConfig config;

        public OllamaClient(OllamaConfig config)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public string ModelName => config.model;
        public string BaseUrl => config.baseUrl;

        // ------------------------------------------------------------------ Sohbet

        public async Task<LlmResult> ChatAsync(IReadOnlyList<ChatMessage> messages, string jsonSchema, CancellationToken cancellation)
        {
            string body = BuildChatBody(messages, jsonSchema);
            var watch = Stopwatch.StartNew();

            using UnityWebRequest request = CreatePost("/api/chat", body, config.timeoutSeconds);
            try
            {
                await SendAsync(request, cancellation);
            }
            catch (OperationCanceledException)
            {
                return LlmResult.Fail(LlmErrorKind.Cancelled, "İstek iptal edildi.", watch.ElapsedMilliseconds);
            }

            long elapsed = watch.ElapsedMilliseconds;
            LlmResult failure = ClassifyFailure(request, elapsed);
            if (failure != null) return failure;

            OllamaChatResponse response;
            try
            {
                response = JsonUtility.FromJson<OllamaChatResponse>(request.downloadHandler.text);
            }
            catch (Exception e)
            {
                return LlmResult.Fail(LlmErrorKind.InvalidResponse, "Sunucu cevabı okunamadı: " + e.Message, elapsed);
            }

            if (response == null || response.message == null || string.IsNullOrEmpty(response.message.content))
            {
                string serverError = response?.error;
                return LlmResult.Fail(LlmErrorKind.InvalidResponse,
                    string.IsNullOrEmpty(serverError) ? "Model boş cevap döndürdü." : serverError, elapsed);
            }

            return LlmResult.Ok(response.message.content, elapsed);
        }

        // ------------------------------------------------------------------ Sağlık kontrolü

        public async Task<LlmHealth> CheckHealthAsync(CancellationToken cancellation)
        {
            using UnityWebRequest request = UnityWebRequest.Get(config.baseUrl + "/api/tags");
            request.timeout = config.healthTimeoutSeconds;

            try
            {
                await SendAsync(request, cancellation);
            }
            catch (OperationCanceledException)
            {
                return LlmHealth.Unknown;
            }

            if (request.result != UnityWebRequest.Result.Success) return LlmHealth.Offline;

            try
            {
                OllamaTagsResponse tags = JsonUtility.FromJson<OllamaTagsResponse>(request.downloadHandler.text);
                if (tags?.models == null) return LlmHealth.ModelMissing;

                foreach (OllamaModelInfo info in tags.models)
                {
                    if (ModelMatches(info?.name) || ModelMatches(info?.model)) return LlmHealth.Online;
                }
                return LlmHealth.ModelMissing;
            }
            catch (Exception)
            {
                return LlmHealth.Offline;
            }
        }

        public async Task WarmUpAsync(CancellationToken cancellation)
        {
            string body = "{\"model\":" + JsonText.Quote(config.model) + ",\"keep_alive\":" + JsonText.Quote(config.keepAlive) + "}";
            using UnityWebRequest request = CreatePost("/api/generate", body, 120);
            try
            {
                await SendAsync(request, cancellation);
                if (request.result == UnityWebRequest.Result.Success) Debug.Log($"[Ollama] Model belleğe yüklendi: {config.model}");
            }
            catch (OperationCanceledException)
            {
                // Oyun kapanıyor; önemli değil.
            }
        }

        // ------------------------------------------------------------------ Yardımcılar

        private string BuildChatBody(IReadOnlyList<ChatMessage> messages, string jsonSchema)
        {
            var sb = new StringBuilder(2048);
            sb.Append("{\"model\":").Append(JsonText.Quote(config.model));
            sb.Append(",\"stream\":false");
            sb.Append(",\"keep_alive\":").Append(JsonText.Quote(config.keepAlive));
            if (!string.IsNullOrEmpty(jsonSchema)) sb.Append(",\"format\":").Append(jsonSchema);
            sb.Append(",\"options\":{\"temperature\":").Append(JsonText.Number(config.temperature));
            sb.Append(",\"num_predict\":").Append(JsonText.Number(config.maxTokens)).Append('}');
            sb.Append(",\"messages\":[");
            for (int i = 0; i < messages.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append("{\"role\":").Append(JsonText.Quote(messages[i].Role));
                sb.Append(",\"content\":").Append(JsonText.Quote(messages[i].Content)).Append('}');
            }
            sb.Append("]}");
            return sb.ToString();
        }

        private UnityWebRequest CreatePost(string path, string body, int timeoutSeconds)
        {
            var request = new UnityWebRequest(config.baseUrl + path, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body)),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = timeoutSeconds
            };
            request.SetRequestHeader("Content-Type", "application/json");
            return request;
        }

        /// <summary>İsteği gönderir ve bitene kadar her karede bir kez kontrol ederek bekler (oyunu dondurmaz).</summary>
        private static async Task SendAsync(UnityWebRequest request, CancellationToken cancellation)
        {
            UnityWebRequestAsyncOperation operation = request.SendWebRequest();
            while (!operation.isDone)
            {
                if (cancellation.IsCancellationRequested)
                {
                    request.Abort();
                    throw new OperationCanceledException(cancellation);
                }
                await Task.Yield();
            }
        }

        private LlmResult ClassifyFailure(UnityWebRequest request, long elapsedMs)
        {
            switch (request.result)
            {
                case UnityWebRequest.Result.Success:
                    return null;

                case UnityWebRequest.Result.ConnectionError:
                    bool timedOut = elapsedMs >= config.timeoutSeconds * 1000L - 250 ||
                                    (request.error ?? "").IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0;
                    return timedOut
                        ? LlmResult.Fail(LlmErrorKind.Timeout, $"{config.timeoutSeconds} sn içinde cevap gelmedi.", elapsedMs)
                        : LlmResult.Fail(LlmErrorKind.ServerUnreachable, $"Ollama'ya ulaşılamadı ({config.baseUrl}): {request.error}", elapsedMs);

                case UnityWebRequest.Result.ProtocolError:
                    if (request.responseCode == 404)
                    {
                        return LlmResult.Fail(LlmErrorKind.ModelNotFound, $"'{config.model}' modeli yüklü değil.", elapsedMs);
                    }
                    return LlmResult.Fail(LlmErrorKind.HttpError, $"HTTP {request.responseCode}: {request.downloadHandler?.text}", elapsedMs);

                default:
                    return LlmResult.Fail(LlmErrorKind.InvalidResponse, request.error ?? "Bilinmeyen hata", elapsedMs);
            }
        }

        private bool ModelMatches(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            if (string.Equals(name, config.model, StringComparison.OrdinalIgnoreCase)) return true;
            // "llama3.2" yazıldıysa "llama3.2:latest" de kabul.
            return !config.model.Contains(":") &&
                   string.Equals(name, config.model + ":latest", StringComparison.OrdinalIgnoreCase);
        }

        // ------------------------------------------------------------------ Ollama cevap şekilleri (JsonUtility için)

        [Serializable]
        private sealed class OllamaChatResponse
        {
            public OllamaMessage message;
            public bool done;
            public string error;
        }

        [Serializable]
        private sealed class OllamaMessage
        {
            public string role;
            public string content;
        }

        [Serializable]
        private sealed class OllamaTagsResponse
        {
            public OllamaModelInfo[] models;
        }

        [Serializable]
        private sealed class OllamaModelInfo
        {
            public string name;
            public string model;
        }
    }
}
