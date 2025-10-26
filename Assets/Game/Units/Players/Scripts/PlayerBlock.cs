using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Units.Players
{
    public class PlayerBlock : MonoBehaviour
    {
        private PlayerStats playerStats;
        private PlayerAnimationController playerAnimationController;
        private PlayerMovement playerMovement;
        private PlayerDash playerDash;
        private PlayerInput playerInput;
        private InputAction blockAction;
        private PlayerAttack playerAttack;

        private bool isBlocking = false;
        public bool IsBlocking => isBlocking;

        private void Start()
        {
            playerStats = GetComponent<PlayerStats>();
            playerAnimationController = GetComponent<PlayerAnimationController>();
            playerMovement = GetComponentInChildren<PlayerMovement>();
            playerDash = GetComponent<PlayerDash>();
            playerAttack = GetComponent<PlayerAttack>();
            playerInput = GetComponent<PlayerInput>();
            SetupInputActions();
        }

        private void SetupInputActions()
        {
            if (playerInput != null)
            {
                blockAction = playerInput.actions["Block"];
                if (blockAction != null)
                {
                    blockAction.performed += OnBlockPerformed;
                    blockAction.canceled += OnBlockCanceled;
                }
            }
        }

        private void OnDestroy()
        {
            if (blockAction != null)
            {
                blockAction.performed -= OnBlockPerformed;
                blockAction.canceled -= OnBlockCanceled;
            }
        }

        private void OnBlockPerformed(InputAction.CallbackContext context)
        {
            isBlocking = true;
            playerAttack.CancelAttack();
            playerDash.CancelDash();
            playerMovement.CancelMoving();
            playerAnimationController.Block();
        }

        private void OnBlockCanceled(InputAction.CallbackContext context)
        {
            isBlocking = false;
            // Immediately break out of block animation and sync to correct state
            playerAnimationController.ForceImmediateStateSync();
        }

        public int ReduceDamageTaken(int damage)
        {
            int defense = playerStats != null ? playerStats.Defense : 0;
            int reduced = Mathf.Max(0, damage - defense);
            return reduced;
        }
    }
}
