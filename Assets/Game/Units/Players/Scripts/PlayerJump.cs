using UnityEngine;

namespace Game.Units.Players
{
    public class PlayerJump : MonoBehaviour
    {
        [Header("Jump Settings")]
        [SerializeField] private float jumpForce = 15f;
        [SerializeField] private float jumpCutMultiplier = 0.5f;
        [SerializeField] private float fallGravityMultiplier = 1.5f;
        [SerializeField] private float maxFallSpeed = 20f;

        [Header("Double Jump")]
        [SerializeField] private bool enableDoubleJump = true;
        [SerializeField] private float doubleJumpForce = 13f;

        [Header("Ground Detection")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private Vector2 groundCheckSize = new Vector2(0.5f, 0.1f);
        [SerializeField] private LayerMask groundLayer;

        [Header("Coyote Time & Jump Buffer")]
        [SerializeField] private float coyoteTime = 0.15f;
        [SerializeField] private float jumpBufferTime = 0.15f;

    private Rigidbody2D rb;
    private PlayerMovement playerMovement;
    private PlayerDash playerDash;

    private bool isGrounded;
    private bool pendingJumpAfterDash = false;
        private float coyoteTimeCounter;
        private float jumpBufferCounter;
        private bool isJumping;
        private int jumpsRemaining;

        public void Initialize(Rigidbody2D rigidbody)
        {
            rb = rigidbody;
            playerMovement = GetComponent<PlayerMovement>();
            playerDash = GetComponent<PlayerDash>();
        }

        public void UpdateJump()
        {
            CheckGrounded();
            UpdateTimers();
            ApplyGravityModifiers();

            // If dash just ended and jump was buffered, try to jump now
            if (pendingJumpAfterDash && playerDash != null && !playerDash.IsDashing)
            {
                pendingJumpAfterDash = false;
                TryPerformJump();
            }
        }

        private void CheckGrounded()
        {
            if (groundCheck != null)
            {
                isGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);
            }
            else
            {
                isGrounded = Physics2D.OverlapBox(transform.position, groundCheckSize, 0f, groundLayer);
            }

            if (playerMovement != null)
            {
                playerMovement.SetGroundedState(isGrounded);
            }

            if (isGrounded && !isJumping)
            {
                coyoteTimeCounter = coyoteTime;
                jumpsRemaining = enableDoubleJump ? 2 : 1;
            }
            else if (isGrounded && isJumping)
            {
                coyoteTimeCounter = coyoteTime;
            }
            else
            {
                coyoteTimeCounter -= Time.deltaTime;
            }
        }

        private void UpdateTimers()
        {
            if (jumpBufferCounter > 0)
            {
                jumpBufferCounter -= Time.deltaTime;
            }
        }

        public void Jump()
        {
            Debug.Log("IKI LOMPAT COK");
            jumpBufferCounter = jumpBufferTime;

            // If dashing, buffer the jump to perform after dash ends
            if (playerDash != null && playerDash.IsDashing)
            {
                pendingJumpAfterDash = true;
                return;
            }

            TryPerformJump();
        }

        private void TryPerformJump()
        {
            if (coyoteTimeCounter > 0f && jumpsRemaining == (enableDoubleJump ? 2 : 1))
            {
                PerformJump(jumpForce);
            }
            else if (enableDoubleJump && jumpsRemaining == 1 && !isGrounded && isJumping)
            {
                PerformJump(doubleJumpForce);
            }
        }

        private void PerformJump(float force)
        {
            Debug.Log("LOMPAT TEMENAN SU");
            if (rb == null) return;

            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * force, ForceMode2D.Impulse);

            jumpsRemaining--;
            coyoteTimeCounter = 0f;
            jumpBufferCounter = 0f;
            isJumping = true;
        }

        public void CancelJump()
        {
            if (rb != null && rb.linearVelocity.y > 0 && isJumping)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
            }
        }

        private void ApplyGravityModifiers()
        {
            if (rb == null) return;

            if (rb.linearVelocity.y < 0)
            {
                rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallGravityMultiplier - 1) * Time.deltaTime;
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -maxFallSpeed));
                
                if (isJumping && rb.linearVelocity.y < -1f)
                {
                    isJumping = false;
                }
            }

            if (jumpBufferCounter > 0 && coyoteTimeCounter > 0f && jumpsRemaining == (enableDoubleJump ? 2 : 1))
            {
                PerformJump(jumpForce);
            }
        }

        public bool IsGrounded()
        {
            return isGrounded;
        }

        public bool IsJumping()
        {
            return isJumping;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Vector3 checkPosition = groundCheck != null ? groundCheck.position : transform.position;
            Gizmos.DrawWireCube(checkPosition, groundCheckSize);
        }
    }
}
