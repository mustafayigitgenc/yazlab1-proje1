using System;
using System.Collections.Generic;
using Gumusdere.Items;
using UnityEngine;

namespace Gumusdere.Npc.Parsing
{
    /// <summary>
    /// Hüsnü Dayı'nın JSON cevabını NpcDecision'a çevirir.
    /// Adımlar:
    ///  1) Metin içinden ilk tam JSON nesnesini çıkar (```json bloğu, başta/sonda fazladan yazı olsa bile).
    ///  2) JsonUtility ile alanları oku. Eksik alanlar özel "işaret" değerleriyle tespit edilir.
    ///  3) Doğrula: "reply" zorunlu; diğer alanlar geçersizse güvenli varsayılana düşer ve uyarı eklenir.
    ///  4) Sayıları kırp (güven değişimi -20..+20), cevabı makul uzunluğa kısalt.
    /// </summary>
    public sealed class NpcResponseParser : IResponseParser<NpcDecision>
    {
        public const int MaxTrustChange = 20;
        public const int MaxReplyLength = 400;

        private const int MissingNumber = int.MinValue;

        /// <summary>Ollama'nın "format" alanına verilen JSON şeması.</summary>
        public const string JsonSchema =
            "{\"type\":\"object\",\"properties\":{" +
            "\"reply\":{\"type\":\"string\"}," +
            "\"emotion\":{\"type\":\"string\",\"enum\":[\"neutral\",\"friendly\",\"suspicious\",\"angry\",\"sad\"]}," +
            "\"trust_change\":{\"type\":\"integer\",\"minimum\":-20,\"maximum\":20}," +
            "\"accepted_gift\":{\"type\":\"string\",\"enum\":[\"none\",\"alabalik\",\"balkabagi\",\"bogurtlen\"]}," +
            "\"action\":{\"type\":\"string\",\"enum\":[\"none\",\"open_mine\",\"kick_out\"]}" +
            "},\"required\":[\"reply\",\"emotion\",\"trust_change\",\"accepted_gift\",\"action\"]}";

        [Serializable]
        private sealed class Dto
        {
            public string reply;
            public string emotion;
            public int trust_change = MissingNumber;
            public string accepted_gift;
            public string action;
        }

        public bool TryParse(string raw, out NpcDecision result, out string error)
        {
            result = null;
            error = null;

            if (string.IsNullOrWhiteSpace(raw))
            {
                error = "Boş cevap";
                return false;
            }

            string json = ExtractJsonObject(raw);
            if (json == null)
            {
                error = "Cevapta JSON nesnesi bulunamadı";
                return false;
            }

            var dto = new Dto();
            try
            {
                JsonUtility.FromJsonOverwrite(json, dto);
            }
            catch (Exception e)
            {
                error = "Geçersiz JSON: " + e.Message;
                return false;
            }

            string reply = dto.reply?.Trim();
            if (string.IsNullOrEmpty(reply))
            {
                error = "\"reply\" alanı eksik ya da boş";
                return false;
            }

            var warnings = new List<string>();
            if (reply.Length > MaxReplyLength)
            {
                reply = reply.Substring(0, MaxReplyLength).TrimEnd() + "...";
                warnings.Add("reply kısaltıldı");
            }

            int trust;
            if (dto.trust_change == MissingNumber)
            {
                trust = 0;
                warnings.Add("trust_change eksik → 0");
            }
            else
            {
                trust = Mathf.Clamp(dto.trust_change, -MaxTrustChange, MaxTrustChange);
                if (trust != dto.trust_change) warnings.Add($"trust_change {dto.trust_change} → {trust}");
            }

            NpcEmotion emotion = ParseEmotion(dto.emotion, warnings);
            NpcAction action = ParseAction(dto.action, warnings);
            ItemType? gift = ParseGift(dto.accepted_gift, warnings);

            result = new NpcDecision(reply, emotion, trust, gift, action, json, warnings);
            return true;
        }

        /// <summary>Metindeki ilk dengeli { ... } bloğunu döndürür (tırnak içindeki parantezleri sayma).</summary>
        public static string ExtractJsonObject(string text)
        {
            int start = text.IndexOf('{');
            if (start < 0) return null;

            int depth = 0;
            bool inString = false;
            bool escaped = false;

            for (int i = start; i < text.Length; i++)
            {
                char c = text[i];
                if (inString)
                {
                    if (escaped) escaped = false;
                    else if (c == '\\') escaped = true;
                    else if (c == '"') inString = false;
                    continue;
                }

                if (c == '"') inString = true;
                else if (c == '{') depth++;
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0) return text.Substring(start, i - start + 1);
                }
            }

            return null;
        }

        private static string Normalize(string value) =>
            string.IsNullOrWhiteSpace(value) ? "" : value.Trim().ToLowerInvariant();

        private static NpcEmotion ParseEmotion(string value, List<string> warnings)
        {
            switch (Normalize(value))
            {
                case "neutral": return NpcEmotion.Neutral;
                case "friendly": return NpcEmotion.Friendly;
                case "suspicious": return NpcEmotion.Suspicious;
                case "angry": return NpcEmotion.Angry;
                case "sad": return NpcEmotion.Sad;
                default:
                    warnings.Add($"emotion '{value}' tanınmadı → neutral");
                    return NpcEmotion.Neutral;
            }
        }

        private static NpcAction ParseAction(string value, List<string> warnings)
        {
            switch (Normalize(value))
            {
                case "none": return NpcAction.None;
                case "open_mine": return NpcAction.OpenMine;
                case "kick_out": return NpcAction.KickOut;
                default:
                    warnings.Add($"action '{value}' tanınmadı → none");
                    return NpcAction.None;
            }
        }

        private static ItemType? ParseGift(string value, List<string> warnings)
        {
            string key = Normalize(value);
            if (key == "" || key == "none") return null;

            foreach (ItemType type in Enum.GetValues(typeof(ItemType)))
            {
                if (ItemInfo.Key(type) == key) return type;
            }

            warnings.Add($"accepted_gift '{value}' tanınmadı → none");
            return null;
        }
    }
}
