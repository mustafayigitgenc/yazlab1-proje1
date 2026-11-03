using System;
using System.Collections.Generic;
using Gumusdere.Companion;
using Gumusdere.Core;
using Gumusdere.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gumusdere.UI
{
    /// <summary>
    /// M tuşuyla açılıp kapanan köy haritası (kuzey yukarıda).
    /// Bölgeleri WorldLayout.MapFeatures'tan çizer; oyuncu, Cemal ve Hüsnü Dayı'nın yerini canlı gösterir.
    /// Harita açıkken oyun durmaz; Esc veya M ile kapanır.
    /// </summary>
    public sealed class WorldMapView : MonoBehaviour
    {
        private const float MapHeight = 740f;
        private const float LegendWidth = 250f;

        private readonly List<(string label, Transform target, Color color)> markers = new();

        private WorldLayout layout;
        private CompanionController companion;
        private Func<bool> canOpen;
        private Texture2D circle;
        private GUIStyle labelStyle;
        private GUIStyle titleStyle;
        private GUIStyle textStyle;
        private GUIStyle panelStyle;

        public bool IsOpen { get; private set; }

        public void Initialize(WorldLayout worldLayout, CompanionController cemal, Func<bool> openAllowed)
        {
            layout = worldLayout;
            companion = cemal;
            canOpen = openAllowed;
        }

        public void AddMarker(string label, Transform target, Color color)
        {
            if (target != null) markers.Add((label, target, color));
        }

        // LateUpdate: GameFlow aynı karede Update'te Esc'yi okurken harita hâlâ "açık" görünür,
        // böylece haritayı kapatan Esc oyunu duraklatmaz.
        private void LateUpdate()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            if (canOpen != null && !canOpen())
            {
                IsOpen = false;
                return;
            }

            if (kb.mKey.wasPressedThisFrame)
            {
                IsOpen = !IsOpen;
            }
            else if (IsOpen && kb.escapeKey.wasPressedThisFrame)
            {
                InputGate.ConsumeEscape();
                IsOpen = false;
            }
        }

        private void OnDestroy()
        {
            if (circle != null) Destroy(circle);
        }

        private void OnGUI()
        {
            if (!IsOpen || layout == null) return;
            EnsureStyles();

            GUI.depth = -10; // diğer arayüzlerin üstünde
            float scale = Mathf.Max(1f, Screen.height / 900f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float width = Screen.width / scale;
            float height = Screen.height / scale;

            // Arka planı karart
            Fill(new Rect(0f, 0f, width, height), new Color(0f, 0f, 0f, 0.55f));

            Rect bounds = layout.MapBounds;
            float mapHeight = Mathf.Min(MapHeight, height - 60f);
            float mapWidth = mapHeight * bounds.width / bounds.height;
            float totalWidth = mapWidth + 16f + LegendWidth;
            var map = new Rect((width - totalWidth) * 0.5f, (height - mapHeight) * 0.5f, mapWidth, mapHeight);

            // Çerçeve + çimen zemin
            Fill(new Rect(map.x - 6f, map.y - 6f, map.width + 12f, map.height + 12f), new Color(0.22f, 0.16f, 0.10f));
            Fill(map, Palette.Grass);

            foreach (MapFeature feature in layout.MapFeatures)
            {
                if (feature.Color.a <= 0f) continue;
                Rect r = ToScreen(feature.Area, map, bounds);
                r = EnsureMinSize(r, 3f);
                GUI.color = feature.Color;
                GUI.DrawTexture(r, feature.Round ? circle : Texture2D.whiteTexture);
            }
            GUI.color = Color.white;

            foreach (MapFeature feature in layout.MapFeatures)
            {
                if (string.IsNullOrEmpty(feature.Label)) continue;
                Vector2 c = ToScreen(feature.Area.center, map, bounds);
                ShadowLabel(new Rect(c.x - 60f, c.y - 10f, 120f, 20f), feature.Label, labelStyle);
            }

            foreach ((string label, Transform target, Color color) in markers)
            {
                if (target == null) continue;
                Vector3 p = target.position;
                Vector2 c = ToScreen(new Vector2(p.x, p.z), map, bounds);
                c.x = Mathf.Clamp(c.x, map.xMin, map.xMax);
                c.y = Mathf.Clamp(c.y, map.yMin, map.yMax);

                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(c.x - 8f, c.y - 8f, 16f, 16f), circle);
                GUI.color = color;
                GUI.DrawTexture(new Rect(c.x - 6f, c.y - 6f, 12f, 12f), circle);
                GUI.color = Color.white;
                ShadowLabel(new Rect(c.x - 50f, c.y - 26f, 100f, 18f), $"<b>{label}</b>", labelStyle);
            }

            DrawLegend(new Rect(map.xMax + 16f, map.y, LegendWidth, Mathf.Min(map.height, 380f)));
        }

        private void DrawLegend(Rect area)
        {
            GUILayout.BeginArea(area, panelStyle);
            GUILayout.Label("Gümüşdere Haritası", titleStyle);
            GUILayout.Space(4f);

            foreach ((string label, Transform target, Color color) in markers)
            {
                LegendRow(color, label, true);
            }
            GUILayout.Space(6f);
            LegendRow(Palette.Soil, "Tarla", false);
            LegendRow(Palette.Water, "Nehir / göl", false);
            LegendRow(Palette.ShallowWater, "Sığ geçit (sadece Cemal)", false);
            LegendRow(Palette.CaveFloor, "Maden", false);
            GUILayout.Space(8f);

            if (layout.Bridge != null)
                GUILayout.Label($"Köprü: {(layout.Bridge.IsLowered ? "indi" : "kalkık")}", textStyle);
            if (layout.Door != null)
                GUILayout.Label($"Maden kapısı: {(layout.Door.IsOpen ? "açık" : "kilitli")}", textStyle);
            if (companion != null)
            {
                string task = companion.CurrentTask != null ? companion.CurrentTask.DisplayName : "boşta";
                GUILayout.Label($"Cemal: {companion.CurrentStateName} ({task})", textStyle);
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label("M / Esc: kapat", textStyle);
            GUILayout.EndArea();
        }

        private void LegendRow(Color color, string text, bool round)
        {
            GUILayout.BeginHorizontal();
            Rect swatch = GUILayoutUtility.GetRect(16f, 16f, GUILayout.Width(16f), GUILayout.Height(18f));
            GUI.color = color;
            GUI.DrawTexture(new Rect(swatch.x, swatch.y + 2f, 14f, 14f), round ? circle : Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUILayout.Space(6f);
            GUILayout.Label(text, textStyle);
            GUILayout.EndHorizontal();
        }

        // ------------------------------------------------------------ Dönüşümler (dünya x/z → ekran, kuzey yukarı)

        private static Vector2 ToScreen(Vector2 world, Rect map, Rect bounds)
        {
            float u = (world.x - bounds.xMin) / bounds.width;
            float v = (world.y - bounds.yMin) / bounds.height;
            return new Vector2(map.x + u * map.width, map.yMax - v * map.height);
        }

        private static Rect ToScreen(Rect world, Rect map, Rect bounds)
        {
            Vector2 a = ToScreen(new Vector2(world.xMin, world.yMax), map, bounds); // sol üst (kuzey-batı)
            Vector2 b = ToScreen(new Vector2(world.xMax, world.yMin), map, bounds); // sağ alt (güney-doğu)
            return Rect.MinMaxRect(a.x, a.y, b.x, b.y);
        }

        private static Rect EnsureMinSize(Rect r, float min)
        {
            if (r.width < min) r = new Rect(r.center.x - min * 0.5f, r.y, min, r.height);
            if (r.height < min) r = new Rect(r.x, r.center.y - min * 0.5f, r.width, min);
            return r;
        }

        // ------------------------------------------------------------ Çizim yardımcıları

        private static void Fill(Rect r, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
        }

        private static void ShadowLabel(Rect r, string text, GUIStyle style)
        {
            Color old = style.normal.textColor;
            style.normal.textColor = new Color(0f, 0f, 0f, 0.85f);
            GUI.Label(new Rect(r.x + 1f, r.y + 1f, r.width, r.height), text, style);
            style.normal.textColor = old;
            GUI.Label(r, text, style);
        }

        private void EnsureStyles()
        {
            if (circle == null) circle = MakeCircle(64);
            if (labelStyle != null) return;

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13,
                richText = true,
                clipping = TextClipping.Overflow,
                normal = { textColor = Color.white }
            };
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            textStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true, normal = { textColor = Color.white } };
            panelStyle = new GUIStyle(GUI.skin.box) { padding = new RectOffset(12, 12, 10, 10) };
        }

        private static Texture2D MakeCircle(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            float r = size * 0.5f;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                    byte a = (byte)(Mathf.Clamp01(r - d) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }
    }
}
