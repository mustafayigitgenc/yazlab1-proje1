using Gumusdere.Items;
using Gumusdere.Mining;
using Gumusdere.World;

namespace Gumusdere.Npc.Actions
{
    /// <summary>Maden kapısını açar. İzni MineEntryRule verir (güven eşiği + baret ve fener).</summary>
    public sealed class OpenMineAction : IGameAction
    {
        private readonly MineDoor door;
        private readonly int requiredTrust;
        private readonly IInventory inventory;

        public OpenMineAction(MineDoor door, int requiredTrust, IInventory inventory)
        {
            this.door = door;
            this.requiredTrust = requiredTrust;
            this.inventory = inventory;
        }

        public string Name => "open_mine";

        public bool CanExecute(int trust, out string reason)
        {
            reason = door == null ? "Kapı yok." : MineEntryRule.Check(trust, requiredTrust, door.IsOpen, inventory);
            return reason == null;
        }

        public void Execute() => door.Open();
    }
}
