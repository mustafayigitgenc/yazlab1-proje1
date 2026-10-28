using Gumusdere.Companion.States;
using Gumusdere.Core;
using UnityEngine;
using UnityEngine.AI;

namespace Gumusdere.Companion
{
    /// <summary>
    /// İşçi Cemal: otonom yapay zekâ yoldaşı.
    /// Hareketi tamamen NavMeshAgent'ın yol bulmasıyla yapılır; karar mantığı bir durum makinesidir.
    /// Bu sınıf sadece bileşenleri bir arada tutar, davranışın kendisi durum sınıflarındadır.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class CompanionController : MonoBehaviour
    {
        public const string DisplayName = "Cemal";

        public NavMeshAgent Agent { get; private set; }
        public Transform Leader { get; private set; }
        public StateMachine Brain { get; } = new();

        public CompanionIdleState IdleState { get; private set; }
        public CompanionFollowState FollowState { get; private set; }

        public string CurrentStateName => Brain.Current?.Name ?? "-";

        public void Initialize(Transform leader)
        {
            Leader = leader;
            Agent = GetComponent<NavMeshAgent>();

            IdleState = new CompanionIdleState(this);
            FollowState = new CompanionFollowState(this);

            Brain.StateChanged += (previous, next) =>
                Debug.Log($"[{DisplayName}] {(previous == null ? "başlangıç" : previous.Name)} → {next.Name}");
            Brain.ChangeState(FollowState);
        }

        private void Update()
        {
            if (Leader == null || Agent == null || !Agent.isOnNavMesh) return;
            Brain.Tick(Time.deltaTime);
        }

        /// <summary>Oyuncunun "bekle / takip et" emri.</summary>
        public void ToggleWait()
        {
            Brain.ChangeState(Brain.Current == IdleState ? FollowState : IdleState);
        }

        /// <summary>Hedefe doğru yumuşakça döner (konuşurken, beklerken).</summary>
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
