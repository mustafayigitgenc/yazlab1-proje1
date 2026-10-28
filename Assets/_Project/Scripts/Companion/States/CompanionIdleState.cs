using Gumusdere.Core;

namespace Gumusdere.Companion.States
{
    /// <summary>Cemal olduğu yerde bekler ve oyuncuya bakar.</summary>
    public sealed class CompanionIdleState : IState
    {
        private readonly CompanionController companion;

        public CompanionIdleState(CompanionController companion)
        {
            this.companion = companion;
        }

        public string Name => "Bekliyor";

        public void Enter()
        {
            if (companion.Agent.isOnNavMesh) companion.Agent.ResetPath();
        }

        public void Tick(float deltaTime)
        {
            companion.FaceTowards(companion.Leader.position, deltaTime);
        }

        public void Exit() { }
    }
}
