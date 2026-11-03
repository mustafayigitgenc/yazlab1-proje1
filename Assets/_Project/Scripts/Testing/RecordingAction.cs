using System;
using Gumusdere.Npc.Actions;

namespace Gumusdere.Testing
{
    /// <summary>
    /// Diyalog testinde gerçek kapı/bekçi yerine takılan eylem. Gerçek eylemlerle aynı eşik kuralını
    /// uygular (OpenMineAction: güven ≥ 70, KickOutAction: güven &lt; 25) ama dünyayı değiştirmez;
    /// sadece "çalıştırıldı mı" bilgisini kaydeder.
    /// </summary>
    public sealed class RecordingAction : IExplainableAction
    {
        private readonly Func<int, string> rule;
        private readonly Func<int, string> explain;

        /// <param name="rule">Güvene bakıp izin yoksa sebebi, izin varsa null döndürür.</param>
        /// <param name="explain">Reddedilince NPC'nin söyleyeceği hazır cümle (gerçek eylemdeki gibi).</param>
        public RecordingAction(string name, Func<int, string> rule, Func<int, string> explain = null)
        {
            Name = name;
            this.rule = rule;
            this.explain = explain;
        }

        public string ExplainRefusal(int trust) => explain?.Invoke(trust) ?? "Kusura bakma evlat, şimdilik bunu yapamam.";

        public string Name { get; }
        public bool Executed { get; private set; }
        public string LastRejection { get; private set; }

        public bool CanExecute(int trust, out string reason)
        {
            reason = rule(trust);
            LastRejection = reason;
            return reason == null;
        }

        public void Execute() => Executed = true;
    }
}
