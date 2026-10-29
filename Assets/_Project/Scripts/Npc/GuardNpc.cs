using Gumusdere.Interaction;
using Gumusdere.Npc.Dialogue;
using UnityEngine;

namespace Gumusdere.Npc
{
    /// <summary>
    /// Maden kapısındaki Hüsnü Dayı. Oyuncu yanına gelip E'ye basınca sohbet penceresi açılır.
    /// Kovulursa oyuncu geri itilir ve bir süre konuşamaz.
    /// </summary>
    public sealed class GuardNpc : MonoBehaviour, IPlayerInteractable
    {
        private const float KickCooldown = 20f;
        private const float KickDistance = 5f;
        private const float LookRange = 7f;

        private NpcDialogueController dialogue;
        private Transform player;
        private float blockedUntil;

        public Vector3 Position => transform.position;
        public float UseRadius => 2.6f;
        public bool IsBlocked => Time.time < blockedUntil;

        public void Initialize(NpcDialogueController dialogueController, Transform playerTransform)
        {
            dialogue = dialogueController;
            player = playerTransform;
        }

        public string GetPrompt(PlayerContext context)
        {
            if (dialogue == null || dialogue.IsOpen) return null;
            if (IsBlocked) return $"{dialogue.Persona.Name} seni dinlemiyor ({blockedUntil - Time.time:0} sn)";
            return $"{dialogue.Persona.Name} ile konuş";
        }

        public void Use(PlayerContext context)
        {
            if (dialogue == null || IsBlocked) return;
            dialogue.Open();
        }

        /// <summary>KickOutAction tarafından çağrılır.</summary>
        public void KickPlayerOut()
        {
            dialogue.Close();
            blockedUntil = Time.time + KickCooldown;

            Vector3 away = player.position - transform.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f) away = Vector3.back;
            Vector3 target = player.position + away.normalized * KickDistance;

            var body = player.GetComponent<CharacterController>();
            if (body != null) body.enabled = false;
            player.position = target;
            if (body != null) body.enabled = true;
        }

        private void Update()
        {
            if (player == null) return;

            Vector3 toPlayer = player.position - transform.position;
            toPlayer.y = 0f;
            if (toPlayer.magnitude > LookRange || toPlayer.sqrMagnitude < 0.01f) return;

            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(toPlayer), 180f * Time.deltaTime);
        }
    }
}
