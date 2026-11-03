using UnityEngine;

namespace Gumusdere.World
{
    /// <summary>Haritadaki tek bir bölge: dünya üzerindeki x/z dikdörtgeni, rengi ve (varsa) etiketi.</summary>
    public readonly struct MapFeature
    {
        public readonly string Label;
        public readonly Rect Area;
        public readonly Color Color;
        public readonly bool Round;

        public MapFeature(string label, Rect area, Color color, bool round = false)
        {
            Label = label;
            Area = area;
            Color = color;
            Round = round;
        }

        /// <summary>Merkez ve boyutla oluşturma kolaylığı.</summary>
        public static MapFeature At(string label, float centerX, float centerZ, float sizeX, float sizeZ, Color color, bool round = false)
            => new(label, new Rect(centerX - sizeX * 0.5f, centerZ - sizeZ * 0.5f, sizeX, sizeZ), color, round);
    }
}
