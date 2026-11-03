using Gumusdere.Companion.Sensing;
using Gumusdere.Core;
using UnityEngine;
using UnityEngine.AI;

namespace Gumusdere.Companion.States
{
    /// <summary>
    /// Cemal görev hedefine NavMesh ile kendi yolunu bulup gider.
    /// Kararlar:
    ///  - Yol hiç yoksa veya yarımsa (PathPartial/PathInvalid) görevi bırakır.
    ///  - Sensör önünde yakın bir engel görürse yavaşlar.
    ///  - 3 sn içinde 1 m bile yer değiştiremezse (gerçekten takıldıysa) yeniden yol hesaplar; 3 denemede olmazsa vazgeçer.
    ///    Not: ilerleme konumdan ölçülür; NavMeshAgent.remainingDistance uzun ve çok köşeli yollarda
    ///    "bilinmiyor" (sonsuz) dönebildiği için takılma kararında kullanılmaz.
    ///  - Hedefe varınca "Kullan" durumuna geçer.
    /// </summary>
    public sealed class CompanionGoToTargetState : IState
    {
        private const float ArriveDistance = 0.7f;
        private const float NormalSpeed = 4f;
        private const float CarefulSpeed = 2f;
        private const float CarefulDistance = 1.2f;
        private const float CarefulAngle = 35f;
        private const float SensorInterval = 0.1f;
        private const float StuckWindow = 3f;
        private const float StuckProgress = 1f;
        private const int MaxRepaths = 3;

        private readonly CompanionController companion;
        private float sensorTimer;
        private float stuckTimer;
        private Vector3 checkpoint;
        private int repathCount;
        private bool announcedWater;

        public CompanionGoToTargetState(CompanionController companion)
        {
            this.companion = companion;
        }

        public string Name => "Hedefe gidiyor";
        public int RepathCount => repathCount;
        public bool IsCareful { get; private set; }

        public void Enter()
        {
            repathCount = 0;
            announcedWater = false;
            RequestPath();
        }

        public void Tick(float deltaTime)
        {
            if (companion.CurrentTask == null)
            {
                companion.Brain.ChangeState(companion.FollowState);
                return;
            }

            NavMeshAgent agent = companion.Agent;
            if (agent.pathPending) return;

            if (agent.pathStatus != NavMeshPathStatus.PathComplete)
            {
                companion.Speaker?.Say("Oraya giden bir yol bulamıyorum ağam!");
                companion.FailTask($"Yol yok ({agent.pathStatus})");
                return;
            }

            UpdateSensor(agent, deltaTime);

            if (!announcedWater && companion.IsInShallowWater())
            {
                announcedWater = true;
                companion.Speaker?.Say("Geçitten geçiyorum, ayaklarım ıslandı!");
            }

            if (HasArrived(agent))
            {
                companion.Brain.ChangeState(companion.InteractState);
                return;
            }

            CheckStuck(agent, deltaTime);
        }

        public void Exit()
        {
            IsCareful = false;
            companion.Agent.speed = NormalSpeed;
        }

        private void RequestPath()
        {
            NavMeshAgent agent = companion.Agent;
            agent.isStopped = false;
            agent.speed = NormalSpeed;
            stuckTimer = 0f;
            checkpoint = companion.transform.position;

            if (!agent.SetDestination(companion.CurrentTask.InteractionPoint))
            {
                companion.FailTask("Hedef NavMesh üzerinde değil");
            }
        }

        private void UpdateSensor(NavMeshAgent agent, float deltaTime)
        {
            sensorTimer -= deltaTime;
            if (sensorTimer > 0f) return;
            sensorTimer = SensorInterval;

            SensorReading reading = companion.Sensor.Scan();
            IsCareful = reading.NearestDistance < CarefulDistance && Mathf.Abs(reading.NearestAngle) <= CarefulAngle;
            agent.speed = IsCareful ? CarefulSpeed : NormalSpeed;
        }

        /// <summary>
        /// Hedefe vardı mı? remainingDistance biliniyorsa ona, bilinmiyorsa (sonsuz) hedefe düz uzaklığa bakılır.
        /// </summary>
        private bool HasArrived(NavMeshAgent agent)
        {
            float remaining = agent.remainingDistance;
            if (!float.IsInfinity(remaining)) return remaining <= ArriveDistance;

            Vector3 d = companion.CurrentTask.InteractionPoint - companion.transform.position;
            d.y = 0f;
            return d.magnitude <= ArriveDistance;
        }

        private void CheckStuck(NavMeshAgent agent, float deltaTime)
        {
            stuckTimer += deltaTime;
            if (stuckTimer < StuckWindow) return;

            Vector3 moved = companion.transform.position - checkpoint;
            moved.y = 0f;
            checkpoint = companion.transform.position;
            stuckTimer = 0f;
            if (moved.magnitude >= StuckProgress) return; // ilerliyor

            repathCount++;
            if (repathCount > MaxRepaths)
            {
                companion.Speaker?.Say("Takıldım kaldım, geri dönüyorum.");
                companion.FailTask("Takıldı");
                return;
            }

            companion.Speaker?.Say("Hmm, başka yoldan deneyeyim...");
            agent.ResetPath();
            RequestPath();
        }
    }
}
