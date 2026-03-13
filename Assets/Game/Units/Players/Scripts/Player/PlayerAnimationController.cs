using UnityEngine;
using System;
using GabrielBigardi.SpriteAnimator;

namespace Game.Units.Players
{
    public class PlayerAnimationController : MonoBehaviour
    {
        // Observer pattern: static events for VFX and other listeners
        public static event Action<AnimState> OnPlayerStateChanged;
        public static event Action OnPlayerHurt;
        public static event Action OnPlayerSkill;
        // Track which attack animation to play next
        [Tooltip("Main player SpriteAnimator")] public SpriteAnimator playerAnim;

        public enum AnimState
        {
            Idle,
            Move,
            Jump,
            Air,
            Land,
            Dash,
            Attack,
            QuickAttack,
            Block
        }

        private AnimState currentState = AnimState.Idle;
        private bool isTransitioning = false;
        private bool forceSyncNextFrame = false;
    

        // Delegates to check if player is moving/grounded (set by PlayerMovement/PlayerJump)
        public Func<bool> IsPlayerMoving;
        public Func<bool> IsPlayerGrounded;
        private bool playAttack1Next = true;
        public bool IsBlocking { get; set; }

        public string PeekNextAttackAnimationName() => playAttack1Next ? "Attack1" : "Attack2";

        public SpriteAnimation PeekNextAttackAnimation()
        {
            if (playerAnim == null)
                return null;

            return playerAnim.GetAnimationByName(PeekNextAttackAnimationName(), false);
        }


        private void Awake()
        {
            if (playerAnim == null)
            {
                Debug.LogWarning("PlayerAnimationController: playerAnim is not assigned!", this);
            }
        }

        private void Update()
        {
            // Only sync to player state if not in a one-shot animation
            if (!isTransitioning)
        {
                SyncToPlayerState();
            }
            else if (forceSyncNextFrame)
            {
                // If a force sync was requested, wait until one-shot completes
                forceSyncNextFrame = false;
            }
        }

        /// <summary>
        /// Call this from other scripts when a gameplay state changes (move input, grounded, etc.)
        /// </summary>
        public void RequestStateSync()
        {
            // Only allow force sync if not in a one-shot; otherwise, will sync after one-shot completes
            forceSyncNextFrame = true;
        }

        /// <summary>
        /// Call 
        /// </summary>
        public void ForceImmediateStateSync()
        {
            isTransitioning = false;
            forceSyncNextFrame = false;
            SyncToPlayerState();
        }

        /// <summary>
        /// Play the given animation state, handling one-shots and transitions.
        /// </summary>
        private void PlayState(AnimState state)
        {
            if (playerAnim == null) return;

            // Attack is always allowed to override any animation (highest priority one-shot)
            if (state != AnimState.Attack)
            {
                // Block all transitions except looping states if a one-shot is playing
                if (isTransitioning)
                {
                    // Only allow Idle, Move, Air to interrupt if not in a one-shot
                    if (state != AnimState.Idle && state != AnimState.Move && state != AnimState.Air)
                        return;
                }
            }

            // Don't replay looping anims if already playing
            if (state == AnimState.Idle || state == AnimState.Move || state == AnimState.Air)
            {
                if (currentState == state)
                    return;
            }
            // For one-shot states (Jump, Land, Dash, Attack), always play even if already in that state

            currentState = state;
            OnPlayerStateChanged?.Invoke(state);
            switch (state)
            {
                case AnimState.Idle:
                    playerAnim.PlayIfNotPlaying("Idle");
                    isTransitioning = false;
                    break;
                case AnimState.Move:
                    playerAnim.PlayIfNotPlaying("Move");
                    isTransitioning = false;
                    break;
                case AnimState.Jump:
                    isTransitioning = true;
                    playerAnim.Play("OnJump").SetOnComplete(() => {
                        isTransitioning = false;
                    });
                    break;
                case AnimState.Air:
                    playerAnim.PlayIfNotPlaying("OnAir");
                    isTransitioning = false;
                    break;
                case AnimState.Land:
                    isTransitioning = true;
                    playerAnim.Play("OnGround").SetOnComplete(() => {
                        isTransitioning = false;
                        // After one-shot, allow state sync
                    });
                    break;
                case AnimState.Dash:
                    isTransitioning = true;
                    playerAnim.Play("Dash").SetOnComplete(() => {
                        isTransitioning = false;
                        // After one-shot, allow state sync
                    });
                    break;
                case AnimState.Attack:
                    isTransitioning = true;
                    string attackAnimName = PeekNextAttackAnimationName();
                    playAttack1Next = !playAttack1Next;
                    playerAnim.Play(attackAnimName).SetOnComplete(() => {
                        isTransitioning = false;
                        // After one-shot, allow state sync
                    });
                    break;
                case AnimState.QuickAttack:
                    isTransitioning = true;
                    playerAnim.Play("QuickAttack").SetOnComplete(() => {
                        isTransitioning = false;
                        // After one-shot, allow state sync
                    });
                    break;
                case AnimState.Block:
                    playerAnim.PlayIfNotPlaying("Block");
                    isTransitioning = false;
                    break;
            }
        }

        /// <summary>
        /// Always sync to the true player state after a one-shot animation.
        /// Priority: Air > Move (if grounded and moving) > Idle (if grounded and not moving)
        /// </summary>
        public void SyncToPlayerState()
        {
            // Check if blocking (set by PlayerBlock)
            if (IsBlocking)
            {
                PlayState(AnimState.Block);
                return;
            }
            if (IsPlayerGrounded == null || IsPlayerMoving == null)
            {
                Debug.LogWarning("PlayerAnimationController: IsPlayerGrounded or IsPlayerMoving delegate not set!", this);
                return;
            }
            if (!IsPlayerGrounded())
            {
                PlayState(AnimState.Air);
            }
            else if (IsPlayerMoving())
            {
                PlayState(AnimState.Move);
            }
            else
            {
                PlayState(AnimState.Idle);
            }
        }

        // Public API for one-shot triggers (should only be called for actual events)
        public void IdleAnim() => PlayState(AnimState.Idle); // Only if you want to force idle
        public void MovingAnim() => PlayState(AnimState.Move); // Only if you want to force move
        public void OnJumpAnim() => PlayState(AnimState.Jump);
        public void OnAirAnim() => PlayState(AnimState.Air);
        public void OnGroundAnim() => PlayState(AnimState.Land);
        public void DashAnim()
        {
            // Forcefully allow dash to override any animation
            isTransitioning = false;
            PlayState(AnimState.Dash);
        }
        public void AttackAnim() => PlayState(AnimState.Attack);
        public void QuickAttack() => PlayState(AnimState.QuickAttack);
        public void Block() => PlayState(AnimState.Block);

        // Example: Call this when the player is hurt
        public void Hurt()
        {
            OnPlayerHurt?.Invoke();
        }

        // Example: Call this when the player uses a skill
        public void Skill()
        {
            OnPlayerSkill?.Invoke();
        }
    }
}

