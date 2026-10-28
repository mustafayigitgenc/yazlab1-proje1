using System.Collections.Generic;
using UnityEngine;

namespace Gumusdere.World
{
    /// <summary>
    /// Renk başına tek bir materyal üretir ve önbellekte tutar.
    /// Projenin URP varsayılan materyali şablon olarak kullanılır; böylece
    /// build'de shader eksik kalma riski olmaz.
    /// </summary>
    public static class MaterialLibrary
    {
        private static readonly Dictionary<Color, Material> Cache = new();
        private static Material template;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Cache.Clear();
            template = null;
        }

        public static Material Get(Color color)
        {
            if (Cache.TryGetValue(color, out Material cached) && cached != null)
            {
                return cached;
            }

            var material = new Material(GetTemplate())
            {
                name = "Mat_" + ColorUtility.ToHtmlStringRGB(color)
            };

            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.15f);

            Cache[color] = material;
            return material;
        }

        private static Material GetTemplate()
        {
            if (template != null) return template;

            // Geçici bir küp oluşturup onun varsayılan materyalini alıyoruz.
            GameObject probe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            template = probe.GetComponent<Renderer>().sharedMaterial;
            Object.DestroyImmediate(probe);

            if (template == null)
            {
                Shader lit = Shader.Find("Universal Render Pipeline/Lit");
                template = new Material(lit != null ? lit : Shader.Find("Standard"));
            }

            return template;
        }
    }
}
