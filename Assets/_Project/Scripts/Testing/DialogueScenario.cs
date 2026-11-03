using System;
using Gumusdere.Items;
using Gumusdere.Npc.Llm;
using Gumusdere.Npc.Parsing;

namespace Gumusdere.Testing
{
    /// <summary>Diyalog testinde bir senaryo: başlangıç durumu, oyuncunun sözü ve beklenen sonuç.</summary>
    public sealed class DialogueScenario
    {
        public string Id;
        public string Title;
        public string PlayerText;
        public string Expectation;
        public int StartTrust = 35;
        public bool MineOpen;

        /// <summary>Madenin ağzındaki destek kirişi sayısı (varsayılan: tamam).</summary>
        public int BeamsPlaced = Gumusdere.Mining.MineEntryRule.RequiredBeams;
        public ItemType[] Inventory = Array.Empty<ItemType>();
        public int Runs = 3;

        /// <summary>null ise gerçek Ollama kullanılır; doluysa (örn. sahte istemci) her tekrar için yeni istemci üretir.</summary>
        public Func<ILLMClient> ClientFactory;

        public Func<DialogueObservation, bool> Pass;

        public bool UsesRealModel => ClientFactory == null;
    }

    /// <summary>Bir senaryo tekrarında gözlenen her şey.</summary>
    public sealed class DialogueObservation
    {
        public NpcDecision Decision;
        public long LatencyMs;
        public int TrustBefore;
        public int TrustAfter;
        public int InventoryBefore;
        public int InventoryAfter;
        public bool Opened;
        public bool Closed;
        public bool Kicked;
        public string LastSystemLine;
        public string Exception;
        public float WallSeconds;
        public int LlmCalls;

        /// <summary>Hediyenin güvene etkisi (zevk tablosundan): toplam değişim − modelin trust_change'i.</summary>
        public int GiftEffect;

        public bool GotDecision => Decision != null;
        public bool GiftAccepted => InventoryAfter < InventoryBefore;
        public NpcAction LlmAction => Decision?.Action ?? NpcAction.None;
        public int LlmTrustChange => Decision?.TrustChange ?? 0;
        public bool NoCrash => Exception == null;
    }
}
