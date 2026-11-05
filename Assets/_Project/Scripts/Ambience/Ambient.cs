using UnityEngine;
using UnityEngine.Rendering;

namespace Gumusdere.Ambience
{
    /// <summary>Süs nesneleri için ortak yardımcılar: çarpışmasız, gölge atmayan küçük şekiller.</summary>
    public static class Ambient
    {
        public static GameObject Decor(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Color color,
            Quaternion? rotation = null)
        {
            GameObject go = World.Shapes.Create(type, name, parent, position, scale, color, false, rotation);
            Renderer r = go.GetComponent<Renderer>();
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = true;
            return go;
        }

        /// <summary>Bloom'un yakalaması için 1'den parlak renk (lambalar, ateş böcekleri, su parıltısı).</summary>
        public static Color Hdr(Color color, float intensity) =>
            new(color.r * intensity, color.g * intensity, color.b * intensity, 1f);
    }
}
