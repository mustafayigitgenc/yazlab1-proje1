using UnityEngine;
using UnityEngine.InputSystem;

namespace Gumusdere.Player
{
    /// <summary>
    /// Köyün sahibi (oyuncu). WASD / ok tuşları ile yürür, Shift ile koşar.
    /// Kamera sabit açıyla baktığı için hareket doğrudan dünya eksenlerindedir (W = kuzey).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private float walkSpeed = 4.5f;
        [SerializeField] private float runSpeed = 7.5f;
        [SerializeField] private float turnSpeed = 720f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float fallResetHeight = -6f;

        private CharacterController body;
        private float verticalVelocity;
        private Vector3 respawnPoint;

        /// <summary>Diyalog gibi durumlarda hareketi kapatmak için.</summary>
        public bool InputEnabled { get; set; } = true;

        public float CurrentSpeed { get; private set; }

        private void Awake()
        {
            body = GetComponent<CharacterController>();
            respawnPoint = transform.position;
        }

        private void Update()
        {
            Vector2 input = InputEnabled ? ReadMoveInput() : Vector2.zero;
            var move = new Vector3(input.x, 0f, input.y);
            if (move.sqrMagnitude > 1f) move.Normalize();

            bool running = InputEnabled && Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
            float speed = running ? runSpeed : walkSpeed;

            if (body.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            verticalVelocity += gravity * Time.deltaTime;

            body.Move((move * speed + Vector3.up * verticalVelocity) * Time.deltaTime);
            CurrentSpeed = move.magnitude * speed;

            if (move.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, Quaternion.LookRotation(move), turnSpeed * Time.deltaTime);
            }

            if (transform.position.y < fallResetHeight) Respawn();
        }

        private void Respawn()
        {
            body.enabled = false;
            transform.position = respawnPoint;
            verticalVelocity = 0f;
            body.enabled = true;
        }

        private static Vector2 ReadMoveInput()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return Vector2.zero;

            float x = 0f, y = 0f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) y -= 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) y += 1f;
            return new Vector2(x, y);
        }
    }
}
