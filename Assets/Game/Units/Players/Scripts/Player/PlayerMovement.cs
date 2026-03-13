using UnityEngine;
using System.Collections;

namespace Game.Units.Players
{
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 8f;
        public float MoveSpeed => moveSpeed;
        [SerializeField] private float acceleration = 50f;
        [SerializeField] private float deceleration = 50f;
        [SerializeField] private float velocityPower = 0.9f;

        [Header("Air Control")]
        [SerializeField] private float airControlMultiplier = 0.7f;

    private Rigidbody2D rb;
    private Vector2 movementInput;
    private bool isGrounded;

    private PlayerAnimationController playerAnimationController;

    // Cancel move transition duration is now local to CancelMovingRoutine
    private bool isCancellingMove = false;
    private Coroutine cancelMoveCoroutine = null;

        public void Initialize(Rigidbody2D rigidbody)
        {
            rb = rigidbody;
        }

        void Start()
        {
            playerAnimationController = GetComponent<PlayerAnimationController>();
            if (playerAnimationController != null)
            {
                playerAnimationController.IsPlayerMoving = IsMoving;
            }
        }

        public void SetMovementInput(Vector2 input)
        {
            if (!isCancellingMove)
                movementInput = input;
        }
        /// <summary>
        /// Interrupts movement and decelerates to zero over speedTransition seconds, ignoring input during that time.
        /// </summary>
        public void CancelMoving()
        {
            if (cancelMoveCoroutine != null)
            {
                StopCoroutine(cancelMoveCoroutine);
                cancelMoveCoroutine = null;
            }
            if (rb != null)
                cancelMoveCoroutine = StartCoroutine(CancelMovingRoutine());
        }

        /// <summary>
        /// Immediately stops the CancelMoving routine and releases input lock.
        /// </summary>
        public void StopCancelMoving()
        {
            Debug.Log("[PlayerMovement] StopCancelMoving: unlocking movement");
            if (cancelMoveCoroutine != null)
            {
                StopCoroutine(cancelMoveCoroutine);
                cancelMoveCoroutine = null;
            }
            isCancellingMove = false;
        }

    private IEnumerator CancelMovingRoutine()
        {
            isCancellingMove = true;
            movementInput = Vector2.zero;
            // Instantly stop horizontal velocity
            if (rb != null)
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            // Wait indefinitely until StopCancelMoving is called
            while (isCancellingMove)
            {
                movementInput = Vector2.zero;
                yield return null;
            }
            cancelMoveCoroutine = null;
        }

        public bool IsDashing { get; set; } = false;

        public void FixedUpdateMovement()
        {
            if (rb == null) return;
            if (IsDashing) return; // Do not apply movement while dashing

            ApplyMovement();
            FlipSprite();
        }

        private bool wasMoving = false;
        private void ApplyMovement()
        {
            bool isCurrentlyMoving = Mathf.Abs(movementInput.x) > 0.01f;
            // Only request state sync if movement state changes while grounded
            if (isGrounded && isCurrentlyMoving != wasMoving && playerAnimationController != null)
            {
                playerAnimationController.RequestStateSync();
            }
            wasMoving = isCurrentlyMoving;

            float targetSpeed = movementInput.x * moveSpeed;
            float speedDifference = targetSpeed - rb.linearVelocity.x;

            float accelerationRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;

            if (!isGrounded)
            {
                accelerationRate *= airControlMultiplier;
            }

            float movement = Mathf.Pow(Mathf.Abs(speedDifference) * accelerationRate, velocityPower) * Mathf.Sign(speedDifference);

            rb.AddForce(movement * Vector2.right);
        }

        private void FlipSprite()
        {
            if (Mathf.Abs(movementInput.x) > 0.01f)
            {
                transform.localScale = new Vector3(Mathf.Sign(movementInput.x), 1, 1);
            }
        }

        public void SetGroundedState(bool grounded)
        {
            if (isGrounded != grounded && playerAnimationController != null)
            {
                playerAnimationController.RequestStateSync();
            }
            isGrounded = grounded;
        }

        public Vector2 GetMovementInput()
        {
            return movementInput;
        }

        public bool IsMoving()
        {
            return Mathf.Abs(movementInput.x) > 0.01f;
        }
    }
}
