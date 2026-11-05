using Gumusdere.Core;
using UnityEngine;

namespace Gumusdere.Ambience
{
    /// <summary>
    /// Karakterlere basit, kodla yapılmış yürüme animasyonu: yürürken adım ritminde hafifçe zıplar ve iki yana sallanır,
    /// dururken nefes alır gibi çok az büyüyüp küçülür. Karakterin bütün görselleri bir "Gorsel" pivotunun altına alınır;
    /// kök nesnedeki fizik ve NavMesh bileşenleri etkilenmez.
    /// </summary>
    public sealed class WalkBob : MonoBehaviour
    {
        private Transform pivot;
        private Transform leftLeg, rightLeg, leftArm, rightArm;
        private Vector3 lastPosition;
        private float phase;
        private float speed;
        private float amplitude = 0.07f;
        private float roll = 4f;
        private float stepsPerMeter = 1.6f;
        private float breathOffset;
        private int stepIndex;

        /// <summary>Her adımda (ayak yere bastığında) tetiklenir; ayak sesi için.</summary>
        public event System.Action Stepped;

        public static WalkBob Attach(GameObject character, float bobHeight = 0.07f, float rollDegrees = 4f, float stepsPerMeter = 1.6f)
        {
            var bob = character.AddComponent<WalkBob>();
            bob.amplitude = bobHeight;
            bob.roll = rollDegrees;
            bob.stepsPerMeter = stepsPerMeter;
            return bob;
        }

        private void Start()
        {
            pivot = new GameObject("Gorsel").transform;
            pivot.SetParent(transform, false);

            // Görselleri (Renderer taşıyan ya da altında Renderer olan çocukları) pivota taşı.
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child == pivot) continue;
                if (child.GetComponentInChildren<Renderer>(true) == null) continue;
                child.SetParent(pivot, true);
            }

            lastPosition = transform.position;
            breathOffset = Random.value * 10f;

            // İnsan karakterlerde kol ve bacak pivotları (HumanoidBody); ineklerde yok, onlarda sadece gövde sallanır.
            leftLeg = transform.FindDeep(Gumusdere.Spawning.HumanoidBody.LeftLeg);
            rightLeg = transform.FindDeep(Gumusdere.Spawning.HumanoidBody.RightLeg);
            leftArm = transform.FindDeep(Gumusdere.Spawning.HumanoidBody.LeftArm);
            rightArm = transform.FindDeep(Gumusdere.Spawning.HumanoidBody.RightArm);
        }

        private void LateUpdate()
        {
            if (pivot == null) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            Vector3 delta = transform.position - lastPosition;
            delta.y = 0f;
            lastPosition = transform.position;
            float measured = Mathf.Min(delta.magnitude / dt, 12f); // ışınlanmalarda zıplamasın
            speed = Mathf.Lerp(speed, measured, 1f - Mathf.Exp(-10f * dt));

            float moving = Mathf.Clamp01(speed / 2.5f);
            phase += speed * stepsPerMeter * Mathf.PI * dt;

            int step = Mathf.FloorToInt(phase / Mathf.PI);
            if (step != stepIndex)
            {
                stepIndex = step;
                if (moving > 0.4f) Stepped?.Invoke();
            }

            float bob = Mathf.Abs(Mathf.Sin(phase)) * amplitude * moving;
            float sway = Mathf.Sin(phase) * roll * moving;
            float breath = 1f + 0.012f * Mathf.Sin(Time.time * 2.2f + breathOffset) * (1f - moving);

            pivot.localPosition = new Vector3(0f, bob, 0f);
            pivot.localRotation = Quaternion.Euler(0f, 0f, sway);
            pivot.localScale = new Vector3(1f, breath, 1f);

            // Adım ritminde bacaklar ileri-geri, kollar ters yönde sallanır.
            float swing = Mathf.Sin(phase) * moving;
            if (leftLeg != null) leftLeg.localRotation = Quaternion.Euler(swing * 28f, 0f, 0f);
            if (rightLeg != null) rightLeg.localRotation = Quaternion.Euler(-swing * 28f, 0f, 0f);
            if (leftArm != null) leftArm.localRotation = Quaternion.Euler(-swing * 24f, 0f, -4f);
            if (rightArm != null) rightArm.localRotation = Quaternion.Euler(swing * 24f, 0f, 4f);
        }
    }
}
