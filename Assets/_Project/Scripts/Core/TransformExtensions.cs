using UnityEngine;

namespace Gumusdere.Core
{
    public static class TransformExtensions
    {
        /// <summary>Adıyla bir alt nesneyi bütün derinliklerde arar (görseller yürüme sallanması için bir alt pivota taşınabiliyor).</summary>
        public static Transform FindDeep(this Transform root, string name)
        {
            if (root == null) return null;
            Transform direct = root.Find(name);
            if (direct != null) return direct;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = root.GetChild(i).FindDeep(name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
