using Gumusdere.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace Gumusdere.Village
{
    /// <summary>
    /// Meradaki inekler: rastgele noktalara yavaşça yürür.
    /// Her biri bir NavMeshAgent olduğu için Cemal'in yolunda hareketli engel oluşturur;
    /// Cemal onlardan otomatik olarak kaçınır (NavMesh yerel kaçınma).
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class AnimalWanderer : MonoBehaviour
    {
        [SerializeField] private float minWait = 2f;
        [SerializeField] private float maxWait = 6f;

        private NavMeshAgent agent;
        private Rect pasture;
        private float nextMoveTime;

        public void Initialize(Rect area)
        {
            pasture = area;
            agent = GetComponent<NavMeshAgent>();
            agent.areaMask = NavAreas.MaskOf(NavAreas.Walkable);
            nextMoveTime = Time.time + Random.Range(0.5f, 2f);
        }

        private void Update()
        {
            if (agent == null || !agent.isOnNavMesh) return;
            if (Time.time < nextMoveTime) return;
            if (agent.pathPending || (agent.hasPath && agent.remainingDistance > 0.5f)) return;

            nextMoveTime = Time.time + Random.Range(minWait, maxWait);
            var candidate = new Vector3(
                Random.Range(pasture.xMin, pasture.xMax),
                0f,
                Random.Range(pasture.yMin, pasture.yMax));

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, agent.areaMask))
            {
                agent.SetDestination(hit.position);
            }
        }
    }
}
