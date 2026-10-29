using System.Collections.Generic;
using Gumusdere.Items;

namespace Gumusdere.Npc.Parsing
{
    /// <summary>NPC'nin oyun durumunu değiştirebilecek eylemi.</summary>
    public enum NpcAction
    {
        None,
        OpenMine,
        KickOut
    }

    public enum NpcEmotion
    {
        Neutral,
        Friendly,
        Suspicious,
        Angry,
        Sad
    }

    /// <summary>
    /// LLM'in JSON çıktısından ayrıştırılmış, doğrulanmış karar. Oyun sadece bu sınıfı kullanır;
    /// ham metne asla bakmaz (kırılgan metin eşleştirmesi yok).
    /// </summary>
    public sealed class NpcDecision
    {
        public NpcDecision(string reply, NpcEmotion emotion, int trustChange, ItemType? acceptedGift,
            NpcAction action, string rawJson, IReadOnlyList<string> warnings)
        {
            Reply = reply;
            Emotion = emotion;
            TrustChange = trustChange;
            AcceptedGift = acceptedGift;
            Action = action;
            RawJson = rawJson;
            Warnings = warnings;
        }

        /// <summary>NPC'nin oyuncuya söylediği söz.</summary>
        public string Reply { get; }

        public NpcEmotion Emotion { get; }

        /// <summary>Güven puanı değişimi (-20..+20 aralığına kırpılmış).</summary>
        public int TrustChange { get; }

        /// <summary>NPC'nin kabul ettiği hediye (yoksa null).</summary>
        public ItemType? AcceptedGift { get; }

        public NpcAction Action { get; }

        /// <summary>Ayrıştırılan JSON (sohbet geçmişine ve loglara bu yazılır).</summary>
        public string RawJson { get; }

        /// <summary>Düzeltilen alanlar (örn. bilinmeyen eylem → none). Raporda ve testlerde gösterilir.</summary>
        public IReadOnlyList<string> Warnings { get; }
    }
}
