using Gumusdere.Core;
using Gumusdere.Interaction;

namespace Gumusdere.Companion.States
{
    /// <summary>
    /// Cemal hedefe döner; sensörüyle hedefi gerçekten gördüğünü doğrular, kısa bir süre çalışır
    /// ve nesneyi kullanır. Hedefi göremezse görevi bırakır.
    /// Çok duraklı görevlerde her durak bitince tekrar "Hedefe git" durumuna döner.
    /// </summary>
    public sealed class CompanionInteractState : IState
    {
        private const float LookTimeout = 3f;
        private const float WorkDuration = 1.2f;

        private readonly CompanionController companion;
        private float lookTimer;
        private float workTimer;
        private bool working;

        public CompanionInteractState(CompanionController companion)
        {
            this.companion = companion;
        }

        public string Name => "Görevi yapıyor";

        public void Enter()
        {
            if (companion.Agent.isOnNavMesh) companion.Agent.ResetPath();
            lookTimer = 0f;
            workTimer = 0f;
            working = false;
        }

        public void Tick(float deltaTime)
        {
            IInteractable task = companion.CurrentTask;
            if (task == null)
            {
                companion.Brain.ChangeState(companion.FollowState);
                return;
            }

            companion.FaceTowards(task.LookPoint, deltaTime);

            if (!working)
            {
                lookTimer += deltaTime;
                if (companion.Sensor.CanSee(task.Root, task.LookPoint))
                {
                    working = true;
                    if (task is not IMultiStepTask) companion.Speaker?.Say("Hemen hallediyorum...");
                }
                else if (lookTimer > LookTimeout)
                {
                    companion.Speaker?.Say("Hedefi göremiyorum ağam!");
                    companion.FailTask("Hedef görünmüyor");
                }
                return;
            }

            workTimer += deltaTime;
            if (workTimer < WorkDuration) return;

            if (!task.IsAvailable)
            {
                companion.FailTask("Hedef artık kullanılamıyor");
                return;
            }

            task.Interact(companion.gameObject);

            // Çok duraklı görevse (örn. sulama) sıradaki durağa git; durak kalmadıysa görev biter.
            if (companion.TryContinueToNextStep()) return;

            if (task is not IMultiStepTask) companion.Speaker?.Say("Tamamdır ağam, iş bitti!");
            companion.CompleteTask();
        }

        public void Exit() { }
    }
}
