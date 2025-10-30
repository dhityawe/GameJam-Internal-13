using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Units.Players
{
    [RequireComponent(typeof(Rigidbody2D), typeof(PlayerInput))]
    public class PlayerController : MonoBehaviour
    {

    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerJump playerJump;
    [SerializeField] private PlayerDash playerDash;
    [SerializeField] private PlayerAttack playerAttack;
    [SerializeField] private PlayerBlock playerBlock;


    private PlayerInput playerInput;
    private Rigidbody2D rb;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction attackAction;


        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            playerInput = GetComponent<PlayerInput>();

            InitializeComponents();
            SetupInputActions();
        }


        private void InitializeComponents()
        {
            if (playerMovement != null)
            {
                playerMovement.Initialize(rb);
            }

            if (playerJump != null)
            {
                playerJump.Initialize(rb);
            }

            if (playerDash != null)
            {
                playerDash.Initialize(rb, playerInput);
            }
        }


        private void SetupInputActions()
        {
            if (playerInput != null)
            {
                moveAction = playerInput.actions["Move"];
                jumpAction = playerInput.actions["Jump"];
                attackAction = playerInput.actions["Attack"];

                jumpAction.performed += OnJumpPerformed;
                jumpAction.canceled += OnJumpCanceled;
                if (attackAction != null)
                {
                    attackAction.performed += OnAttackPerformed;
                }
                else
                {
                    Debug.LogWarning("[PlayerController] Attack action not found!");
                }
            }
        }


        private void OnDestroy()
        {
            if (jumpAction != null)
            {
                jumpAction.performed -= OnJumpPerformed;
                jumpAction.canceled -= OnJumpCanceled;
            }
            if (attackAction != null)
            {
                attackAction.performed -= OnAttackPerformed;
            }
        }
        private void OnAttackPerformed(InputAction.CallbackContext context)
        {
            if (playerBlock != null && playerBlock.IsBlocking) return;
            if (playerAttack != null)
            {
                playerAttack.Attack();
            }
        }

        private void Update()
        {
            if (playerBlock != null && playerBlock.IsBlocking)
            {
                // While blocking, prevent all actions
                if (playerMovement != null) playerMovement.SetMovementInput(Vector2.zero);
                return;
            }
            if (playerDash != null)
            {
                playerDash.UpdateDash();
            }
            bool wasGrounded = false;
            if (playerJump != null)
            {
                wasGrounded = playerJump.IsGrounded();
                playerJump.UpdateJump();
            }
            // Reset dash when landing
            if (playerDash != null && playerJump != null)
            {
                if (playerJump.IsGrounded())
                {
                    playerDash.OnLanded();
                }
            }
            if (playerMovement != null && moveAction != null)
            {
                // Disable movement input while dashing
                if (playerDash != null && playerDash.IsDashing)
                {
                    playerMovement.SetMovementInput(Vector2.zero);
                }
                else
                {
                    playerMovement.SetMovementInput(moveAction.ReadValue<Vector2>());
                }
            }
        }

        private void FixedUpdate()
        {
            if (playerMovement != null)
            {
                // Tell movement if dashing
                playerMovement.IsDashing = (playerDash != null && playerDash.IsDashing);
                playerMovement.FixedUpdateMovement();
            }
        }

        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            if (playerBlock != null && playerBlock.IsBlocking) return;
            // If dashing, cancel dash and jump immediately
            if (playerDash != null && playerDash.IsDashing)
            {
                playerDash.CancelDash();
            }
            if (playerJump != null)
            {
                playerJump.Jump();
            }
        }

        private void OnJumpCanceled(InputAction.CallbackContext context)
        {
            if (playerJump != null)
            {
                playerJump.CancelJump();
            }
        }
    }
}
