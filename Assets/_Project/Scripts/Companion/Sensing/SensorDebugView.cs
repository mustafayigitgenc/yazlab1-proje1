using Gumusdere.Core;
using Gumusdere.World;
using UnityEngine;

namespace Gumusdere.Companion.Sensing
{
    /// <summary>
    /// F1 açıkken sensör ışınlarını oyun ekranında çizer:
    /// yeşil = engel yok, turuncu = engel var, kırmızı = engel çok yakın.
    /// </summary>
    public sealed class SensorDebugView : MonoBehaviour
    {
        private const float DangerDistance = 1.2f;

        private RaycastSensor sensor;
        private LineRenderer[] lines;
        private Material clear, blocked, danger;

        public void Initialize(RaycastSensor targetSensor)
        {
            sensor = targetSensor;
            clear = MaterialLibrary.Get(new Color(0.3f, 0.95f, 0.4f));
            blocked = MaterialLibrary.Get(new Color(1f, 0.6f, 0.1f));
            danger = MaterialLibrary.Get(new Color(1f, 0.15f, 0.15f));

            int count = sensor.Latest.IsValid ? sensor.Latest.Distances.Length : 7;
            lines = new LineRenderer[count];
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("SensorIsini_" + i);
                go.transform.SetParent(transform, false);
                LineRenderer line = go.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.positionCount = 2;
                line.widthMultiplier = 0.05f;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line.enabled = false;
                lines[i] = line;
            }
        }

        private void LateUpdate()
        {
            if (sensor == null || lines == null) return;

            SensorReading reading = sensor.Latest;
            bool show = DebugView.Enabled && reading.IsValid;
            for (int i = 0; i < lines.Length; i++)
            {
                LineRenderer line = lines[i];
                line.enabled = show && i < reading.Distances.Length;
                if (!line.enabled) continue;

                float d = reading.Distances[i];
                Vector3 eye = sensor.Eye;
                line.SetPosition(0, eye);
                line.SetPosition(1, eye + sensor.RayDirection(i) * d);
                line.sharedMaterial = d >= reading.Range ? clear : d < DangerDistance ? danger : blocked;
            }
        }
    }
}
