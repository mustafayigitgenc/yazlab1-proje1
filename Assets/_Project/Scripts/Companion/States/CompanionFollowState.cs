using Gumusdere.Core;
using UnityEngine;
using UnityEngine.AI;

namespace Gumusdere.Companion.States
{
    /// <summary>
    /// Cemal oyuncuyu birkaç adım geriden takip eder.
    /// Hedef nokta her 0,25 sn'de yeniden seçilir ve yol NavMesh üzerinde hesaplanır;
    /// arada engel (çit, ağaç, inek) varsa ajan kendisi etrafından dolaşır.
    /// </summary>
    public sealed class CompanionFollowState : IState
    {
        private const float StopDistance = 2.2f;
        private const float RunDistance = 7f;
        private const float WalkSpeed = 3.5f;
        private const float RunSpeed = 6f;
        private const float RepathInterval = 0.25f;

        private readonly CompanionController companion;
        private float repathTimer;

        public CompanionFollowState(CompanionController companion)
        {
            this.companion = companion;
        }

        public string Name => "Takip ediyor";

        public void Enter()
        {
            repathTimer = 0f;
            companion.Agent.isStopped = false;
        }

        public void Tick(float deltaTime)
        {
            NavMeshAgent agent = companion.Agent;
            Transform leader = companion.Leader;
            float distance = Vector3.Distance(companion.transform.position, leader.position);

            agent.speed = distance > RunDistance ? RunSpeed : WalkSpeed;

            if (distance <= StopDistance)
            {
                if (agent.hasPath) agent.ResetPath();
                companion.FaceTowards(leader.position, deltaTime);
                return;
            }

            repathTimer -= deltaTime;
            if (repathTimer > 0f) return;
            repathTimer = RepathInterval;

            // Oyuncunun biraz arkasındaki noktayı hedefle; orası NavMesh dışındaysa oyuncunun kendisini.
            Vector3 behind = leader.position - leader.forward * 1.5f;
            if (NavMesh.SamplePosition(behind, out NavMeshHit hit, 2f, agent.areaMask) ||
                NavMesh.SamplePosition(leader.position, out hit, 4f, agent.areaMask))
            {
                agent.SetDestination(hit.position);
            }
        }

        public void Exit() { }
    }
}
