using System.Collections.Generic;
using Gumusdere.Companion;
using Gumusdere.Core;
using Gumusdere.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gumusdere.Player
{
    /// <summary>
    /// Oyuncunun tuşla verdiği emirler.
    ///   F  : yakındaki görevi Cemal'e ver (örn. köprü başındayken "köprüyü indir")
    ///   G  : Cemal boştayken bekle / takip et; görevdeyken geri çağır
    ///   F1 : debug görünümü (yol çizgisi, sensör ışınları, durum bilgisi)
    /// </summary>
    public sealed class PlayerCommandInput : MonoBehaviour
    {
        private readonly List<IInteractable> tasks = new();
        private ICommandable companion;

        /// <summary>Şu an verilebilecek emir (yoksa null). Arayüz bunu gösterir.</summary>
        public IInteractable AvailableTask { get; private set; }

        public void Initialize(ICommandable targetCompanion, IEnumerable<IInteractable> companionTasks)
        {
            companion = targetCompanion;
            tasks.AddRange(companionTasks);
        }

        private void Update()
        {
            AvailableTask = FindAvailableTask();

            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            if (kb.f1Key.wasPressedThisFrame) DebugView.Toggle();
            if (companion == null) return;

            if (kb.fKey.wasPressedThisFrame && AvailableTask != null)
            {
                companion.TryAssignTask(AvailableTask);
            }

            if (kb.gKey.wasPressedThisFrame)
            {
                if (companion.IsBusy) companion.Recall();
                else companion.ToggleWait();
            }
        }

        private IInteractable FindAvailableTask()
        {
            if (companion == null || companion.IsBusy) return null;
            foreach (IInteractable task in tasks)
            {
                if (task.IsAvailable && task.CanBeRequestedFrom(transform.position)) return task;
            }
            return null;
        }
    }
}
