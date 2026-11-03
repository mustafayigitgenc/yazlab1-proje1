using System;
using Gumusdere.Items;
using Gumusdere.Mining;
using Gumusdere.World;

namespace Gumusdere.Npc.Actions
{
    /// <summary>Maden kapısını açar. İzni MineEntryRule verir (güven eşiği + baret ve fener + destek kirişleri).</summary>
    public sealed class OpenMineAction : IExplainableAction
    {
        private readonly MineDoor door;
        private readonly int requiredTrust;
        private readonly IInventory inventory;
        private readonly Func<int> beamsPlaced;

        public OpenMineAction(MineDoor door, int requiredTrust, IInventory inventory, Func<int> beamsPlaced = null)
        {
            this.door = door;
            this.requiredTrust = requiredTrust;
            this.inventory = inventory;
            this.beamsPlaced = beamsPlaced ?? (() => MineEntryRule.RequiredBeams);
        }

        public string Name => "open_mine";

        public bool CanExecute(int trust, out string reason)
        {
            reason = door == null ? "Kapı yok." : MineEntryRule.Check(trust, requiredTrust, door.IsOpen, inventory, beamsPlaced());
            return reason == null;
        }

        public void Execute() => door.Open();

        public string ExplainRefusal(int trust) =>
            MineEntryRule.ExplainToPlayer(trust, requiredTrust, door != null && door.IsOpen, inventory, beamsPlaced());
    }
}
