using Gumusdere.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace Gumusdere.Village
{
    /// <summary>
    /// İnek: kendi bölgesinde rastgele noktalara yavaşça yürür (başıboş). Cemal yanına gelince durur (Held),
    /// sonra Cemal'in peşine takılır (Follow) ve ahıra girince ahırın içinde dolaşır (Penned).
    /// Her inek bir NavMeshAgent olduğu için Cemal'in yolunda hareketli engel oluşturur;
    /// Cemal onlardan otomatik olarak kaçınır (NavMesh yerel kaçınma).
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class AnimalWanderer : MonoBehaviour
    {
        public enum Mode { Wander, Held, Follow, Penned }

        private const float WanderSpeed = 1.1f;
        private const float FollowSpeed = 3.8f;

        [SerializeField] private float minWait = 2f;
        [SerializeField] private float maxWait = 6f;

        private NavMeshAgent agent;
        private Rect area;
        private Transform leader;
        private float nextMoveTime;

        public Mode Current { get; private set; } = Mode.Wander;
        public bool IsPenned => Current == Mode.Penned;

        public void Initialize(Rect wanderArea)
        {
            area = wanderArea;
            agent = GetComponent<NavMeshAgent>();
            agent.areaMask = NavAreas.MaskOf(NavAreas.Walkable);
            nextMoveTime = Time.time + Random.Range(0.5f, 2f);
        }

        /// <summary>Olduğu yerde durur (Cemal yanına gelirken).</summary>
        public void Hold()
        {
            Current = Mode.Held;
            if (agent.isOnNavMesh) agent.ResetPath();
        }

        /// <summary>Verilen kişinin (Cemal) peşine takılır.</summary>
        public void Follow(Transform target)
        {
            leader = target;
            Current = Mode.Follow;
            agent.speed = FollowSpeed;
            agent.acceleration = 8f;
            agent.stoppingDistance = 1.6f;
            nextMoveTime = 0f;
        }

        /// <summary>Ahıra girer ve bundan sonra sadece ahırın içinde dolaşır.</summary>
        public void Pen(Rect pen)
        {
            area = pen;
            Current = Mode.Penned;
            agent.speed = WanderSpeed;
            agent.acceleration = 4f;
            agent.stoppingDistance = 0f;
            nextMoveTime = 0f;
        }

        /// <summary>Görev yarıda kalırsa tekrar başıboş dolaşmaya döner.</summary>
        public void Release()
        {
            if (Current == Mode.Penned) return;
            Current = Mode.Wander;
            agent.speed = WanderSpeed;
            agent.acceleration = 4f;
            agent.stoppingDistance = 0f;
        }

        private void Update()
        {
            if (agent == null || !agent.isOnNavMesh) return;

            switch (Current)
            {
                case Mode.Held:
                    return;

                case Mode.Follow:
                    if (leader == null || Time.time < nextMoveTime) return;
                    nextMoveTime = Time.time + 0.3f;
                    agent.SetDestination(leader.position - leader.forward * 1.8f);
                    return;
            }

            if (Time.time < nextMoveTime) return;
            if (agent.pathPending || (agent.hasPath && agent.remainingDistance > 0.5f)) return;

            nextMoveTime = Time.time + Random.Range(minWait, maxWait);
            var candidate = new Vector3(Random.Range(area.xMin, area.xMax), 0f, Random.Range(area.yMin, area.yMax));
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, agent.areaMask))
            {
                agent.SetDestination(hit.position);
            }
        }
    }
}
