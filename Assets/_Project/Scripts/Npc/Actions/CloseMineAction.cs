using Gumusdere.World;

namespace Gumusdere.Npc.Actions
{
    /// <summary>
    /// Maden kapısını geri kapatır (oyuncu isterse ya da NPC'yi çok kızdırırsa).
    /// Kapı zaten kapalıysa hiçbir şey yapılmaz.
    /// </summary>
    public sealed class CloseMineAction : IGameAction
    {
        private readonly MineDoor door;

        public CloseMineAction(MineDoor door)
        {
            this.door = door;
        }

        public string Name => "close_mine";

        public bool CanExecute(int trust, out string reason)
        {
            if (door == null || !door.IsOpen)
            {
                reason = "Kapı zaten kapalı";
                return false;
            }

            reason = null;
            return true;
        }

        public void Execute() => door.Close();
    }
}
