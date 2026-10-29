using System;
using Gumusdere.Companion.Sensing;
using Gumusdere.Companion.States;
using Gumusdere.Core;
using Gumusdere.Interaction;
using Gumusdere.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace Gumusdere.Companion
{
    /// <summary>
    /// İşçi Cemal: otonom yapay zekâ yoldaşı.
    /// Hareketi tamamen NavMeshAgent'ın yol bulmasıyla yapılır; karar mantığı bir durum makinesidir:
    ///   Takip ↔ Bekle,  Takip → Hedefe git → Kullan → Geri dön → Takip
    ///   (Hedefe giderken takılırsa yeniden yol arar; yol yoksa görevi bırakıp geri döner.)
    /// Bu sınıf sadece bileşenleri bir arada tutar; davranışın kendisi durum sınıflarındadır.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class CompanionController : MonoBehaviour, ICommandable
    {
        public const string DisplayName = "Cemal";

        public NavMeshAgent Agent { get; private set; }
        public Transform Leader { get; private set; }
        public IObstacleSensor Sensor { get; private set; }
        public ISpeaker Speaker { get; private set; }
        public StateMachine Brain { get; } = new();

        public CompanionIdleState IdleState { get; private set; }
        public CompanionFollowState FollowState { get; private set; }
        public CompanionGoToTargetState GoToTargetState { get; private set; }
        public CompanionInteractState InteractState { get; private set; }
        public CompanionReturnState ReturnState { get; private set; }

        /// <summary>Şu an üzerinde çalışılan görev (yoksa null).</summary>
        public IInteractable CurrentTask { get; private set; }

        public string CurrentStateName => Brain.Current?.Name ?? "-";
        public bool IsBusy => CurrentTask != null || Brain.Current == ReturnState;

        /// <summary>Görev başarıyla bitince (hedef adı, süre sn).</summary>
        public event Action<string, float> TaskSucceeded;

        /// <summary>Görev başarısız olunca (hedef adı, sebep).</summary>
        public event Action<string, string> TaskFailed;

        private float taskStartTime;
        private float idleScanTimer;

        public void Initialize(Transform leader, IObstacleSensor sensor, ISpeaker speaker)
        {
            Leader = leader;
            Sensor = sensor;
            Speaker = speaker;
            Agent = GetComponent<NavMeshAgent>();

            IdleState = new CompanionIdleState(this);
            FollowState = new CompanionFollowState(this);
            GoToTargetState = new CompanionGoToTargetState(this);
            InteractState = new CompanionInteractState(this);
            ReturnState = new CompanionReturnState(this);

            Brain.StateChanged += (previous, next) =>
                Debug.Log($"[{DisplayName}] {(previous == null ? "başlangıç" : previous.Name)} → {next.Name}");
            Brain.ChangeState(FollowState);
        }

        private void Update()
        {
            if (Leader == null || Agent == null || !Agent.isOnNavMesh) return;
            Brain.Tick(Time.deltaTime);

            // Görev dışındayken de sensör ara sıra okunur (debug ekranı güncel kalsın).
            idleScanTimer -= Time.deltaTime;
            if (Brain.Current != GoToTargetState && idleScanTimer <= 0f && Sensor != null)
            {
                idleScanTimer = 0.2f;
                Sensor.Scan();
            }
        }

        // ------------------------------------------------------------ ICommandable

        public bool TryAssignTask(IInteractable target)
        {
            if (target == null || !target.IsAvailable || IsBusy) return false;

            CurrentTask = target;
            taskStartTime = Time.time;
            Speaker?.Say($"Tamam ağam, {target.DisplayName.ToLowerInvariant()} işini hallediyorum!");
            Brain.ChangeState(GoToTargetState);
            return true;
        }

        public void Recall()
        {
            if (CurrentTask != null) FailTask("Oyuncu geri çağırdı");
            else Brain.ChangeState(FollowState);
        }

        public void ToggleWait()
        {
            if (IsBusy) return;
            Brain.ChangeState(Brain.Current == IdleState ? FollowState : IdleState);
        }

        // ------------------------------------------------------------ Durumların kullandığı yardımcılar

        /// <summary>Görev tamamlandı: olayı bildir, oyuncuya dön.</summary>
        public void CompleteTask()
        {
            string taskName = CurrentTask?.DisplayName ?? "?";
            float duration = Time.time - taskStartTime;
            CurrentTask = null;
            TaskSucceeded?.Invoke(taskName, duration);
            Brain.ChangeState(ReturnState);
        }

        /// <summary>Görev yapılamadı: sebebi söyle, oyuncuya dön.</summary>
        public void FailTask(string reason)
        {
            string taskName = CurrentTask?.DisplayName ?? "?";
            CurrentTask = null;
            Debug.LogWarning($"[{DisplayName}] Görev başarısız ({taskName}): {reason}");
            TaskFailed?.Invoke(taskName, reason);
            Brain.ChangeState(ReturnState);
        }

        /// <summary>Cemal şu an sığ suyun içinde mi (NavMesh alanına bakarak).</summary>
        public bool IsInShallowWater()
        {
            return NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 0.5f, NavMesh.AllAreas)
                   && (hit.mask & NavAreas.MaskOf(NavAreas.ShallowWater)) != 0;
        }

        /// <summary>Hedefe yumuşakça döner.</summary>
        public void FaceTowards(Vector3 point, float deltaTime)
        {
            Vector3 dir = point - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) return;
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, Quaternion.LookRotation(dir), 360f * deltaTime);
        }
    }
}
