using Gumusdere.World;
using UnityEngine;

namespace Gumusdere.Spawning
{
    /// <summary>
    /// İnsan karakterlerin ortak gövdesi: ayrı bacaklar (ayakkabılı), kemerli gövde, omuzdan sallanan kollar ve eller,
    /// boyun, baş, gözler, kaşlar ve saç. Kol ve bacaklar birer pivotun altındadır; WalkBob yürürken bunları sallar.
    /// Baş ve şapka yükseklikleri eski gövdeyle aynıdır, yani şapka, baret ve ekipman yerinde kalır.
    /// </summary>
    public static class HumanoidBody
    {
        public const string LeftLeg = "Bacak_Sol_Pivot";
        public const string RightLeg = "Bacak_Sag_Pivot";
        public const string LeftArm = "Kol_Sol_Pivot";
        public const string RightArm = "Kol_Sag_Pivot";

        private static readonly Color Shoes = new(0.22f, 0.15f, 0.1f);
        private static readonly Color Belt = new(0.25f, 0.17f, 0.1f);
        private static readonly Color EyeColor = new(0.08f, 0.06f, 0.05f);

        public struct Style
        {
            public Color Shirt;
            public Color Trousers;
            public Color Skin;
            public Color Hair;
            public float Bulk;      // gövde genişliği (1 = normal)
            public bool Hair_On;    // saç çizilsin mi (şapka/baret çoğunu örter)
        }

        public static void Build(Transform root, Style s)
        {
            float bulk = s.Bulk <= 0f ? 1f : s.Bulk;

            // Bacaklar
            Leg(root, LeftLeg, -0.13f, s.Trousers);
            Leg(root, RightLeg, 0.13f, s.Trousers);

            // Gövde ve kemer
            Shapes.Capsule("Govde", root, new Vector3(0f, 0.93f, 0f), new Vector3(0.6f * bulk, 0.4f, 0.42f * bulk), s.Shirt, false);
            Shapes.Box("Kemer", root, new Vector3(0f, 0.64f, 0f), new Vector3(0.52f * bulk, 0.08f, 0.38f * bulk), Belt, false);

            // Kollar
            Arm(root, LeftArm, -0.33f * bulk - 0.02f, s.Shirt, s.Skin);
            Arm(root, RightArm, 0.33f * bulk + 0.02f, s.Shirt, s.Skin);

            // Boyun ve baş
            Shapes.Cylinder("Boyun", root, new Vector3(0f, 1.3f, 0f), new Vector3(0.15f, 0.05f, 0.15f), s.Skin, false);
            Shapes.Sphere("Kafa", root, new Vector3(0f, 1.5f, 0f), Vector3.one * 0.48f, s.Skin, false);
            if (s.Hair_On)
            {
                Shapes.Sphere("Sac", root, new Vector3(0f, 1.55f, -0.05f), new Vector3(0.5f, 0.44f, 0.48f), s.Hair, false);
            }

            // Yüz: gözler ve kaşlar
            foreach (float x in new[] { -0.085f, 0.085f })
            {
                Shapes.Sphere("Goz", root, new Vector3(x, 1.53f, 0.205f), new Vector3(0.06f, 0.07f, 0.04f), EyeColor, false);
                Shapes.Box("Kas", root, new Vector3(x, 1.605f, 0.21f), new Vector3(0.09f, 0.022f, 0.03f), s.Hair, false);
            }
        }

        private static void Leg(Transform root, string name, float x, Color trousers)
        {
            var hip = new GameObject(name).transform;
            hip.SetParent(root, false);
            hip.localPosition = new Vector3(x, 0.62f, 0f);
            Shapes.Box("Bacak", hip, new Vector3(0f, -0.28f, 0f), new Vector3(0.19f, 0.56f, 0.22f), trousers, false);
            Shapes.Box("Ayakkabi", hip, new Vector3(0f, -0.57f, 0.04f), new Vector3(0.21f, 0.1f, 0.3f), Shoes, false);
        }

        private static void Arm(Transform root, string name, float x, Color sleeve, Color skin)
        {
            var shoulder = new GameObject(name).transform;
            shoulder.SetParent(root, false);
            shoulder.localPosition = new Vector3(x, 1.2f, 0f);
            Shapes.Capsule("Kol", shoulder, new Vector3(0f, -0.25f, 0f), new Vector3(0.15f, 0.25f, 0.15f), sleeve, false);
            Shapes.Sphere("El", shoulder, new Vector3(0f, -0.52f, 0f), Vector3.one * 0.13f, skin, false);
        }
    }
}
