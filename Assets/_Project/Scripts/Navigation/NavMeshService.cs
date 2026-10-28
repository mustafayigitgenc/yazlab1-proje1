using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Gumusdere.Navigation
{
    /// <summary>
    /// Haritanın NavMesh'ini oyun açılırken koddan hesaplar (Unity'nin yerleşik NavMeshBuilder API'si).
    /// Ek paket gerekmez. Köprü inmesi gibi değişikliklerde RebuildAsync ile ana akışı
    /// dondurmadan yeniden hesaplanır.
    /// </summary>
    public sealed class NavMeshService : IDisposable
    {
        private readonly Bounds bounds;
        private readonly List<NavMeshBuildMarkup> markups = new();
        private readonly List<NavMeshBuildSource> sources = new();

        private NavMeshData data;
        private NavMeshDataInstance instance;

        public NavMeshService(Bounds bounds)
        {
            this.bounds = bounds;
        }

        public bool IsBuilt => data != null;

        /// <summary>Verilen nesne ve tüm çocukları NavMesh hesabına katılmaz (oyuncu, görünmez duvarlar, hayvanlar).</summary>
        public void Ignore(Transform root)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                markups.Add(new NavMeshBuildMarkup { root = t, ignoreFromBuild = true });
            }
        }

        /// <summary>Verilen nesne ve çocuklarının NavMesh alan türünü değiştirir (örn. sığ su).</summary>
        public void SetArea(Transform root, int area)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                markups.Add(new NavMeshBuildMarkup { root = t, overrideArea = true, area = area });
            }
        }

        public void Build()
        {
            NavMesh.SetAreaCost(NavAreas.ShallowWater, NavAreas.ShallowWaterCost);

            CollectSources();
            NavMeshBuildSettings settings = NavMesh.GetSettingsByID(0);
            data = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, Vector3.zero, Quaternion.identity);
            if (data == null)
            {
                Debug.LogError("[NavMesh] NavMesh oluşturulamadı.");
                return;
            }

            instance = NavMesh.AddNavMeshData(data);
            Debug.Log($"[NavMesh] Hazır: {sources.Count} kaynak nesne kullanıldı.");
        }

        /// <summary>Sahne değiştiğinde NavMesh'i arka planda günceller.</summary>
        public AsyncOperation RebuildAsync()
        {
            if (data == null) return null;
            CollectSources();
            return NavMeshBuilder.UpdateNavMeshDataAsync(data, NavMesh.GetSettingsByID(0), sources, bounds);
        }

        private void CollectSources()
        {
            sources.Clear();
            NavMeshBuilder.CollectSources(
                bounds,
                ~0,
                NavMeshCollectGeometry.PhysicsColliders,
                NavAreas.Walkable,
                markups,
                sources);
        }

        public void Dispose()
        {
            if (instance.valid) instance.Remove();
            data = null;
        }
    }
}
