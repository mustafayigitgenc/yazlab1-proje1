using Gumusdere.World;

namespace Gumusdere.Npc.Actions
{
    /// <summary>Maden kapısını açar. Sadece güven eşiği aşıldıysa izin verilir.</summary>
    public sealed class OpenMineAction : IGameAction
    {
        private readonly MineDoor door;
        private readonly int requiredTrust;

        public OpenMineAction(MineDoor door, int requiredTrust)
        {
            this.door = door;
            this.requiredTrust = requiredTrust;
        }

        public string Name => "open_mine";

        public bool CanExecute(int trust, out string reason)
        {
            if (door == null || door.IsOpen)
            {
                reason = "Kapı zaten açık";
                return false;
            }

            if (trust < requiredTrust)
            {
                reason = $"Güven yetersiz ({trust} < {requiredTrust})";
                return false;
            }

            reason = null;
            return true;
        }

        public void Execute() => door.Open();
    }
}
