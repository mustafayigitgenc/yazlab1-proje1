using Gumusdere.Companion;
using Gumusdere.Core;
using Gumusdere.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gumusdere.Player
{
    /// <summary>
    /// Oyuncunun tuşla verdiği emirler.
    ///   G  : Cemal'e "bekle / takip et"
    ///   F1 : debug görünümü (yol çizgisi, durum bilgisi)
    ///   F2 : (sadece test) köprüyü indir — 2. adımda bunu Cemal yapacak
    /// </summary>
    public sealed class PlayerCommandInput : MonoBehaviour
    {
        private CompanionController companion;
        private Drawbridge bridge;

        public void Initialize(CompanionController targetCompanion, Drawbridge targetBridge)
        {
            companion = targetCompanion;
            bridge = targetBridge;
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            if (kb.gKey.wasPressedThisFrame && companion != null) companion.ToggleWait();
            if (kb.f1Key.wasPressedThisFrame) DebugView.Toggle();
            if (kb.f2Key.wasPressedThisFrame && bridge != null) bridge.Lower();
        }
    }
}
