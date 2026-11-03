using System;
using Gumusdere.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gumusdere.UI
{
    /// <summary>
    /// Ekranın ortasında açılan pencerelerin ortak davranışı (bakkal, ilan panosu):
    ///  - açıkken oyuncu yürüyemez,
    ///  - Esc veya E ile kapanır (açan E tuşu pencereyi aynı karede kapatmaz),
    ///  - Esc ile kapanırken oyun duraklatılmaz (LateUpdate + InputGate).
    /// Alt sınıflar sadece içeriği çizer (Template Method).
    /// </summary>
    public abstract class ModalWindow : MonoBehaviour
    {
        private Action<bool> setControls;
        private int openedFrame = -1;
        private Texture2D background;

        protected GUIStyle TitleStyle { get; private set; }
        protected GUIStyle TextStyle { get; private set; }
        protected GUIStyle SmallStyle { get; private set; }
        protected GUIStyle ButtonStyle { get; private set; }

        public bool IsOpen { get; private set; }

        protected abstract Vector2 PanelSize { get; }
        protected abstract void DrawContent();
        protected virtual void OnOpened() { }
        protected virtual void OnClosed() { }

        protected void SetControlsCallback(Action<bool> controlsSetter) => setControls = controlsSetter;

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            openedFrame = Time.frameCount;
            setControls?.Invoke(false);
            OnOpened();
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            setControls?.Invoke(true);
            OnClosed();
        }

        protected void LateUpdate()
        {
            if (!IsOpen || Time.frameCount == openedFrame) return;
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            if (kb.escapeKey.wasPressedThisFrame)
            {
                InputGate.ConsumeEscape();
                Close();
            }
            else if (kb.eKey.wasPressedThisFrame)
            {
                Close();
            }
        }

        protected void OnGUI()
        {
            if (!IsOpen) return;
            EnsureStyles();

            float scale = Mathf.Max(1f, Screen.height / 900f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float w = Screen.width / scale;
            float h = Screen.height / scale;

            Vector2 size = PanelSize;
            var panel = new Rect((w - size.x) * 0.5f, (h - size.y) * 0.5f, size.x, size.y);
            Color old = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(panel, background);
            GUI.color = old;

            GUILayout.BeginArea(new Rect(panel.x + 20f, panel.y + 14f, panel.width - 40f, panel.height - 28f));
            DrawContent();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Esc / E: kapat", SmallStyle);
            GUILayout.EndArea();
        }

        protected void OnDestroy()
        {
            if (background != null) Destroy(background);
        }

        private void EnsureStyles()
        {
            if (TitleStyle != null) return;
            TitleStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, richText = true, normal = { textColor = Color.white } };
            TextStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, richText = true, wordWrap = true, normal = { textColor = Color.white } };
            SmallStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, richText = true, wordWrap = true, normal = { textColor = new Color(0.85f, 0.85f, 0.85f) } };
            ButtonStyle = new GUIStyle(GUI.skin.button) { fontSize = 15 };
            background = new Texture2D(1, 1) { hideFlags = HideFlags.DontSave };
            background.SetPixel(0, 0, new Color(0.10f, 0.08f, 0.06f, 0.94f));
            background.Apply();
        }
    }
}
