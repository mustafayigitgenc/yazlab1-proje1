using UnityEngine;

namespace Gumusdere.World
{
    /// <summary>
    /// Unity'nin hazır şekilleriyle (küp, silindir, küre, kapsül) nesne üretmek için yardımcılar.
    /// "solid" false ise çarpışma (collider) kaldırılır; nesne sadece görüntüdür.
    /// </summary>
    public static class Shapes
    {
        public static GameObject Create(
            PrimitiveType type,
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Color color,
            bool solid = true,
            Quaternion? localRotation = null)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = localRotation ?? Quaternion.identity;
            go.transform.localScale = localScale;
            go.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Get(color);

            if (!solid)
            {
                Object.DestroyImmediate(go.GetComponent<Collider>());
            }

            return go;
        }

        public static GameObject Box(string name, Transform parent, Vector3 position, Vector3 scale, Color color,
            bool solid = true, Quaternion? rotation = null)
            => Create(PrimitiveType.Cube, name, parent, position, scale, color, solid, rotation);

        public static GameObject Cylinder(string name, Transform parent, Vector3 position, Vector3 scale, Color color,
            bool solid = true, Quaternion? rotation = null)
            => Create(PrimitiveType.Cylinder, name, parent, position, scale, color, solid, rotation);

        public static GameObject Sphere(string name, Transform parent, Vector3 position, Vector3 scale, Color color,
            bool solid = true)
            => Create(PrimitiveType.Sphere, name, parent, position, scale, color, solid);

        public static GameObject Capsule(string name, Transform parent, Vector3 position, Vector3 scale, Color color,
            bool solid = true)
            => Create(PrimitiveType.Capsule, name, parent, position, scale, color, solid);

        /// <summary>Görünmez, sadece çarpışması olan kutu (duvar, sınır vb.).</summary>
        public static GameObject InvisibleBox(string name, Transform parent, Vector3 position, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.AddComponent<BoxCollider>().size = size;
            return go;
        }

        /// <summary>
        /// Görünmez silindir engel (örn. göl). Kapsül collider eliptik şekilleri bozduğu için
        /// silindirin kendi mesh'i MeshCollider olarak kullanılır.
        /// </summary>
        public static GameObject InvisibleCylinder(string name, Transform parent, Vector3 position, Vector3 scale)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;

            Mesh mesh = go.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            Object.DestroyImmediate(go.GetComponent<MeshRenderer>());
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
            return go;
        }
    }
}
