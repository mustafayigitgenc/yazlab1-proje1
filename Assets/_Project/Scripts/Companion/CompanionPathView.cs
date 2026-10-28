using Gumusdere.Core;
using Gumusdere.World;
using UnityEngine;
using UnityEngine.AI;

namespace Gumusdere.Companion
{
    /// <summary>
    /// Debug görünümü açıkken (F1) Cemal'in NavMesh'te hesapladığı yolu sarı çizgiyle gösterir.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class CompanionPathView : MonoBehaviour
    {
        private const float LineHeight = 0.25f;

        private NavMeshAgent agent;
        private LineRenderer line;
        private Vector3[] buffer = new Vector3[32];

        public void Initialize(NavMeshAgent targetAgent)
        {
            agent = targetAgent;
            line = GetComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.widthMultiplier = 0.12f;
            line.sharedMaterial = MaterialLibrary.Get(Palette.DebugPath);
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.positionCount = 0;
        }

        private void LateUpdate()
        {
            if (agent == null) return;

            bool show = DebugView.Enabled && agent.isOnNavMesh && agent.hasPath;
            line.enabled = show;
            if (!show) return;

            NavMeshPath path = agent.path;
            int count = path.GetCornersNonAlloc(buffer);
            if (count == buffer.Length)
            {
                buffer = new Vector3[buffer.Length * 2];
                count = path.GetCornersNonAlloc(buffer);
            }

            line.positionCount = count;
            for (int i = 0; i < count; i++)
            {
                line.SetPosition(i, buffer[i] + Vector3.up * LineHeight);
            }
        }
    }
}
