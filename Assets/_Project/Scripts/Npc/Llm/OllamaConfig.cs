using System;
using System.IO;
using UnityEngine;

namespace Gumusdere.Npc.Llm
{
    /// <summary>
    /// Ollama bağlantı ayarları. Varsayılanlar koddadır; build'in yanındaki
    /// StreamingAssets/ollama_config.json dosyası varsa oradaki değerler kullanılır.
    /// Böylece model veya adres (örn. ekip arkadaşının bilgisayarı) yeniden build almadan değiştirilebilir.
    /// </summary>
    [Serializable]
    public sealed class OllamaConfig
    {
        public const string FileName = "ollama_config.json";

        public string baseUrl = "http://localhost:11434";
        public string model = "llama3.2:3b";
        public int timeoutSeconds = 45;
        public int healthTimeoutSeconds = 3;
        public float temperature = 0.8f;
        public int maxTokens = 300;
        public int retryCount = 1;
        public string keepAlive = "15m";

        public static OllamaConfig LoadOrDefault()
        {
            var config = new OllamaConfig();
            string path = Path.Combine(Application.streamingAssetsPath, FileName);

            try
            {
                if (File.Exists(path))
                {
                    JsonUtility.FromJsonOverwrite(File.ReadAllText(path), config);
                    Debug.Log($"[Ollama] Ayarlar okundu: {path}");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Ollama] Ayar dosyası okunamadı, varsayılanlar kullanılıyor: {e.Message}");
            }

            config.baseUrl = string.IsNullOrWhiteSpace(config.baseUrl) ? "http://localhost:11434" : config.baseUrl.TrimEnd('/');
            config.timeoutSeconds = Mathf.Clamp(config.timeoutSeconds, 5, 300);
            config.retryCount = Mathf.Clamp(config.retryCount, 0, 3);
            return config;
        }
    }
}
