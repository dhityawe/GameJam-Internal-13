using UnityEngine;
using System;
using UnityEngine.InputSystem;
namespace Game.Units.Players
{
    public class PlayerDash : MonoBehaviour
    {
        [Header("Dash Settings")]
        [SerializeField] private float dashDuration = 0.2f;
        [SerializeField] private float dashCooldown = 0.3f;
        [SerializeField] private float dashSpeedMultiplier = 2f;
        [SerializeField] private int maxDashes = 1;
        [SerializeField] private float dashBufferTime = 0.1f;
        // [SerializeField] private float dashEndDeceleration = 40f;

        private PlayerAnimationController playerAnimationController;
        private PlayerAttack playerAttack;

        private Rigidbody2D rb;
        private float originalGravityScale;
        private PlayerMovement playerMovement;
        private PlayerJump playerJump;
        private PlayerInput playerInput;
        private InputAction dashAction;
        private InputAction moveAction;

        public event Action OnStartDash;
        public event Action OnEndDash;

        private bool isDashing = false;
        private float dashTimer = 0f;
        private float dashCooldownTimer = 0f;
        private int dashesLeft;
        private Vector2 dashDirection;
        private float dashBufferCounter = 0f;

        public bool IsDashing => isDashing;

        public void Initialize(Rigidbody2D rigidbody, PlayerInput input)
        {
            rb = rigidbody;
            playerInput = input;
            playerMovement = GetComponent<PlayerMovement>();
            playerJump = GetComponent<PlayerJump>();
            playerAttack = GetComponent<PlayerAttack>();
            playerAnimationController = GetComponent<PlayerAnimationController>();
            dashesLeft = maxDashes;
            SetupInputActions();
        }

        private void SetupInputActions()
        {
            if (playerInput != null)
            {
                dashAction = playerInput.actions["Dash"];
                moveAction = playerInput.actions["Move"];
                dashAction.performed += OnDashPerformed;
            }
        }

        private void OnDestroy()
        {
            if (dashAction != null)
            {
                dashAction.performed -= OnDashPerformed;
            }
        }

        private void OnDashPerformed(InputAction.CallbackContext context)
        {
            dashBufferCounter = dashBufferTime;
        }

        public void UpdateDash()
        {
            if (dashCooldownTimer > 0)
                dashCooldownTimer -= Time.deltaTime;

            if (dashBufferCounter > 0)
                dashBufferCounter -= Time.deltaTime;

            if (!isDashing && dashBufferCounter > 0 && dashCooldownTimer <= 0 && dashesLeft > 0)
            {
                StartDash();
                dashBufferCounter = 0f;
            }

            if (isDashing)
            {
                // Ensure gravity is always 0 during dash
                if (rb != null)
                {
                    rb.gravityScale = 0f;
                }
                // Decelerate dash speed smoothly from dashSpeedMultiplier to 1x over dashDuration
                if (rb != null && playerMovement != null)
                {
                    float t = 1f - (dashTimer / dashDuration); // 0 at start, 1 at end
                    float currentMultiplier = Mathf.Lerp(dashSpeedMultiplier, 1f, t);
                    Vector2 boostedDir = dashDirection;
                    // Boost horizontal dashes
                    if (Mathf.Abs(dashDirection.x) > 0.01f && Mathf.Abs(dashDirection.y) < 0.7f) // mostly horizontal
                    {
                        boostedDir.x *= 2f; // boost factor, tweak as needed
                        boostedDir = boostedDir.normalized;
                    }
                    rb.linearVelocity = boostedDir * (playerMovement.MoveSpeed * currentMultiplier);
                }
                dashTimer -= Time.deltaTime;
                if (dashTimer <= 0)
                {
                    EndDash();
                }
            }
        }

        private void StartDash()
        {
            OnStartDash?.Invoke();
            isDashing = true;
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;
            dashesLeft--;
            playerAttack.CancelAttack();

            // Play dash animation
            if (playerAnimationController != null)
                playerAnimationController.DashAnim();

            // 8-directional dash based on input
            dashDirection = moveAction.ReadValue<Vector2>();
            if (dashDirection == Vector2.zero)
                dashDirection = Vector2.right * transform.localScale.x; // Default to facing direction
            dashDirection = dashDirection.normalized;

            // Store and disable gravity
            if (rb != null)
            {
                originalGravityScale = rb.gravityScale;
                rb.gravityScale = 0f;
                rb.linearVelocity = dashDirection * (playerMovement != null ? playerMovement.MoveSpeed * dashSpeedMultiplier : 20f);
            }
        }
    

        private void EndDash()
        {
            OnEndDash?.Invoke();
            isDashing = false;
            // Restore gravity
            if (rb != null)
            {
                rb.gravityScale = originalGravityScale;
            }
            // Release any movement lock that may have been set during the dash (e.g. attack pressed mid-dash)
            if (playerMovement != null)
                playerMovement.StopCancelMoving();
            if (playerAnimationController != null)
            {
                bool grounded = playerJump != null ? playerJump.IsGrounded() : false;
                if (grounded)
                    playerAnimationController.IdleAnim();
                else
                    playerAnimationController.OnAirAnim();
            }
        }

        public void CancelDash()
        {
            if (isDashing)
            {
                EndDash();
            }
        }
        
        // Call this from PlayerController when grounded
        public void OnLanded()
        {
            dashesLeft = maxDashes;
            // Do not reset dashCooldownTimer here; let it finish naturally
        }

        // (Removed duplicate OnDrawGizmos)

        public void ResetDash()
        {
            dashesLeft = maxDashes;
        }
    }
}
