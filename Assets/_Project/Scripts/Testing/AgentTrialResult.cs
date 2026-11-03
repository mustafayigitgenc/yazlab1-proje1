using UnityEngine;

namespace Gumusdere.Testing
{
    /// <summary>Ajan testindeki tek bir denemenin ölçümleri.</summary>
    public sealed class AgentTrialResult
    {
        public int Index;
        public string Target;
        public Vector3 Start;
        public float StraightDistance;
        public float PathLength;
        public bool Success;
        public float Seconds;
        public int Repaths;
        public bool CrossedFord;
        public bool Careful;
        public string Reason;
    }
}
