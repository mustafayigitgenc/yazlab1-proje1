using Gumusdere.Core;
using UnityEngine;
using UnityEngine.AI;

namespace Gumusdere.Companion.States
{
    /// <summary>
    /// Görev bitince (ya da bırakılınca) Cemal oyuncunun yanına döner, sonra takibe geçer.
    /// Köprü indiyse NavMesh güncellendiği için dönüşte köprüyü kullanır (geçitten daha "ucuz" yol).
    /// </summary>
    public sealed class CompanionReturnState : IState
    {
        private const float CloseEnough = 3f;
        private const float ReturnSpeed = 5f;
        private const float RepathInterval = 0.5f;

        private readonly CompanionController companion;
        private float repathTimer;

        public CompanionReturnState(CompanionController companion)
        {
            this.companion = companion;
        }

        public string Name => "Geri dönüyor";

        public void Enter()
        {
            repathTimer = 0f;
            companion.Agent.isStopped = false;
            companion.Agent.speed = ReturnSpeed;
        }

        public void Tick(float deltaTime)
        {
            Vector3 leader = companion.Leader.position;
            if (Vector3.Distance(companion.transform.position, leader) <= CloseEnough)
            {
                companion.Brain.ChangeState(companion.FollowState);
                return;
            }

            repathTimer -= deltaTime;
            if (repathTimer > 0f) return;
            repathTimer = RepathInterval;

            if (NavMesh.SamplePosition(leader, out NavMeshHit hit, 4f, companion.Agent.areaMask))
            {
                companion.Agent.SetDestination(hit.position);
            }
        }

        public void Exit() { }
    }
}
