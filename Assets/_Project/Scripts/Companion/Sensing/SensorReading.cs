namespace Gumusdere.Companion.Sensing
{
    /// <summary>
    /// Bir sensör okumasının sonucu: her ışının açısı ve çarptığı engele mesafesi.
    /// </summary>
    public readonly struct SensorReading
    {
        /// <summary>Işın açıları (derece; 0 = tam ileri, eksi = sol, artı = sağ).</summary>
        public readonly float[] Angles;

        /// <summary>Her ışının engele mesafesi (metre). Engel yoksa sensör menzili.</summary>
        public readonly float[] Distances;

        public readonly float Range;
        public readonly int NearestIndex;

        public SensorReading(float[] angles, float[] distances, float range)
        {
            Angles = angles;
            Distances = distances;
            Range = range;

            NearestIndex = 0;
            for (int i = 1; i < distances.Length; i++)
            {
                if (distances[i] < distances[NearestIndex]) NearestIndex = i;
            }
        }

        public bool IsValid => Distances != null && Distances.Length > 0;
        public bool AnyHit => IsValid && Distances[NearestIndex] < Range;
        public float NearestDistance => IsValid ? Distances[NearestIndex] : float.PositiveInfinity;
        public float NearestAngle => IsValid ? Angles[NearestIndex] : 0f;
    }
}
