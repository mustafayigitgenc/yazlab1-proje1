namespace Gumusdere.Npc.Actions
{
    /// <summary>
    /// NPC oyuncuyu kovar: diyalog kapanır, oyuncu geri itilir ve bir süre konuşamaz.
    /// Sadece güven çok düşükse izin verilir; model durup dururken saldırgan davranamaz.
    /// </summary>
    public sealed class KickOutAction : IGameAction
    {
        private readonly GuardNpc guard;
        private readonly int maxTrust;

        public KickOutAction(GuardNpc guard, int maxTrust)
        {
            this.guard = guard;
            this.maxTrust = maxTrust;
        }

        public string Name => "kick_out";

        public bool CanExecute(int trust, out string reason)
        {
            if (trust >= maxTrust)
            {
                reason = $"Güven hâlâ yüksek ({trust} >= {maxTrust})";
                return false;
            }

            reason = null;
            return true;
        }

        public void Execute() => guard.KickPlayerOut();
    }
}
