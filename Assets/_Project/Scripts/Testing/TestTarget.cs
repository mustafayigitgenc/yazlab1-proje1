using Gumusdere.Interaction;
using UnityEngine;

namespace Gumusdere.Testing
{
    /// <summary>
    /// Ajan testinde kullanılan yan etkisiz görev hedefi. Cemal'e gerçek bir görev gibi verilir;
    /// aynı durum makinesi, yol bulma ve sensör kullanılır ama dünyada hiçbir şey değişmez
    /// (köprü inmez, göçük kalkmaz), böylece her deneme aynı koşullarda yapılır.
    /// </summary>
    public sealed class TestTarget : MonoBehaviour, IInteractable
    {
        public string DisplayName { get; private set; } = "Test hedefi";
        public string CommandText => "(test)";
        public Vector3 InteractionPoint => transform.position;
        public Transform Root => transform;
        public Vector3 LookPoint => transform.position + Vector3.up * 0.5f;
        public bool IsAvailable => true;

        /// <summary>Hedefin neyi ölçtüğü (rapora yazılır).</summary>
        public string Note { get; private set; }

        public int Reached { get; private set; }

        public static TestTarget Create(Transform parent, string name, Vector3 position, string note)
        {
            var go = new GameObject("TestHedefi_" + name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var target = go.AddComponent<TestTarget>();
            target.DisplayName = name;
            target.Note = note;
            return target;
        }

        public bool CanBeRequestedFrom(Vector3 playerPosition) => false; // oyuncu F ile veremez, sadece test koşucusu verir

        public void Interact(GameObject actor) => Reached++;
    }
}
