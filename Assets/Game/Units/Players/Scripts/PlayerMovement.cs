using UnityEngine;

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

        [Header("Reference")]
        [SerializeField] private PlayerAnimationController playerAnimationController;

        public void Initialize(Rigidbody2D rigidbody)
        {
            rb = rigidbody;
        }

        public void SetMovementInput(Vector2 input)
        {
            movementInput = input;
        }

        public bool IsDashing { get; set; } = false;

        public void FixedUpdateMovement()
        {
            if (rb == null) return;
            if (IsDashing) return; // Do not apply movement while dashing

            ApplyMovement();
            FlipSprite();
        }

        private void ApplyMovement()
        {
            if (Mathf.Abs(movementInput.x) > 0.01f)
            {
                playerAnimationController.MovingAnim();
            }
            else
            {
                playerAnimationController.IdleAnim();
            }
            
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
