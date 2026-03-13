using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Game.Units.Players
{
    // Handles player attack logic
    public class PlayerAttack : MonoBehaviour
    {

        [Header("Attack Settings")]
        [SerializeField] private float attackCooldown = 0.3f;
        public float AttackCooldown => attackCooldown;
        private float attackTimer = 0f;

        private PlayerStats playerStats;
        private PlayerAnimationController playerAnimationController;
        private PlayerMovement playerMovement;
        

        #region Attack Frame
        [Serializable]
        public struct AttackFrameInfo
        {
            public string animationName;
            public int unlockFrame;
            [Tooltip("List of frame indices (0-based) where attack hit should occur.")]
            public List<int> attackHitFrames;
        }
        [Header("Attack Animation Timing")]
        [Tooltip("List of attack animation names and their unlock frames.")]
        [SerializeField] private List<AttackFrameInfo> attackUnlockFrames = new List<AttackFrameInfo>();
        public int GetUnlockFrameForAnimation(string animName)
        {
            foreach (var info in attackUnlockFrames)
            {
                if (info.animationName == animName)
                    return info.unlockFrame;
            }
            return 4; // fallback default
        }
        public List<int> GetHitFramesForAnimation(string animName)
        {
            foreach (var info in attackUnlockFrames)
            {
                if (info.animationName == animName)
                    return info.attackHitFrames;
            }
            return null;
        }
        #endregion

        #region Attack Config
        [Header("Attack Area")]
        [SerializeField] private Transform attackPoint;
        [SerializeField] private float attackRange = 0.5f;
        [SerializeField] private LayerMask enemyLayers;
        private void Awake()
        {
            playerStats = GetComponent<PlayerStats>();
        }

        void Start()
        {
            playerAnimationController = GetComponent<PlayerAnimationController>();
            playerMovement = GetComponentInChildren<PlayerMovement>();
        }

        private void Update()
        {
            if (attackTimer > 0)
                attackTimer -= Time.deltaTime;
        }

        public bool CanAttack()
        {
            return attackTimer <= 0f;
        }

        public void Attack()
        {
            if (!CanAttack()) return;
            attackTimer = attackCooldown;

            // Lock movement at the start of the attack
            playerMovement.CancelMoving();

            // Setup per-frame unlock event for attack animation
            string attackAnimName = "Attack1";
            if (playerAnimationController != null)
            {
                // Alternate between Attack1 and Attack2 if needed
                var animCtrlType = playerAnimationController.GetType();
                var playAttack1NextField = animCtrlType.GetField("playAttack1Next", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (playAttack1NextField != null && playAttack1NextField.GetValue(playerAnimationController) is bool playAttack1Next)
                {
                    attackAnimName = playAttack1Next ? "Attack1" : "Attack2";
                }

                int unlockFrame = GetUnlockFrameForAnimation(attackAnimName);
                var hitFrames = GetHitFramesForAnimation(attackAnimName);
                var frameEvents = new Dictionary<int, List<Action>>();
                // Unlock movement event
                frameEvents[unlockFrame] = new List<Action> {
                    () => {
                        Debug.Log($"[PlayerAttack] Unlock frame event fired for {attackAnimName} at frame {unlockFrame}");
                        playerMovement.StopCancelMoving();
                    }
                };
                // Attack hit events
                if (hitFrames != null)
                {
                    foreach (var hitFrame in hitFrames)
                    {
                        if (!frameEvents.ContainsKey(hitFrame))
                            frameEvents[hitFrame] = new List<Action>();
                        int frameCopy = hitFrame;
                        frameEvents[hitFrame].Add(() => OnAttackHit(attackAnimName, frameCopy));
                    }
                }
                playerAnimationController.playerAnim.SetAnimationFrameEvents(attackAnimName, frameEvents);

            }

            playerAnimationController.AttackAnim();

            if (attackPoint == null)
            {
                Debug.LogWarning("[PlayerAttack] AttackPoint not assigned!");
                return;
            }
        }

        // Called when attack hit frame is reached
        private void OnAttackHit(string animName, int frame)
        {
            Debug.Log("<color=yellow>[PlayerAttack]</color> Attack hit event fired for " + animName + " at frame " + frame);
            // Place your hit logic here (e.g., damage enemies)
            StartCoroutine(HitStop());
        }

        /// <summary>
        /// Interrupts the current attack, resetting the cooldown and allowing immediate re-attack or interruption.
        /// </summary>
        public void CancelAttack()
        {
            attackTimer = 0f;
            // Stop any movement cancel coroutine to avoid interfering with dash
            if (playerMovement != null)
            {
                playerMovement.StopCancelMoving();
            }
            // Stop attack animation and sync to correct state
            if (playerAnimationController != null)
            {
                playerAnimationController.RequestStateSync();
            }
            // Optionally: stop attack effects here if needed
        }
        #endregion

        #region Attack Feedback
        IEnumerator HitStop()
        {
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(0.03f);
            Time.timeScale = 1f;
        }
        #endregion

        #region Debug Area
        private void OnDrawGizmosSelected()
        {
            if (attackPoint != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(attackPoint.position, attackRange);
            }
        }
        #endregion
    }
}
