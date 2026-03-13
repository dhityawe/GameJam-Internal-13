using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using GabrielBigardi.SpriteAnimator;

namespace Game.Units.Players
{
    // Handles player attack logic
    public class PlayerAttack : MonoBehaviour
    {

        [Header("Attack Settings")]
        [SerializeField] private float attackCooldown = 0.3f;
        public float AttackCooldown => attackCooldown;
        private float attackTimer = 0f;
        [SerializeField] private bool enableDebugLogs = false;
        [SerializeField] private int hitDetectionBufferSize = 32;

        private PlayerStats playerStats;
        private PlayerAnimationController playerAnimationController;
        private PlayerMovement playerMovement;
        private Rigidbody2D rb;
        private readonly Dictionary<string, Dictionary<int, List<Action>>> cachedFrameEventMaps = new();
        private Collider2D[] hitDetectionResults;
        private ContactFilter2D hitDetectionFilter;
        private Coroutine hitStopCoroutine;
        
        #region Attack Config
        [Header("Attack Target Filter")]
        [SerializeField] private LayerMask enemyLayers;
        private void Awake()
        {
            playerStats = GetComponent<PlayerStats>();
            rb = GetComponent<Rigidbody2D>();
            hitDetectionResults = new Collider2D[Mathf.Max(4, hitDetectionBufferSize)];
            ConfigureHitDetectionFilter();
        }

        private void OnValidate()
        {
            hitDetectionBufferSize = Mathf.Max(4, hitDetectionBufferSize);
            ConfigureHitDetectionFilter();
        }

        private void ConfigureHitDetectionFilter()
        {
            hitDetectionFilter.useTriggers = true;
            hitDetectionFilter.useLayerMask = true;
            hitDetectionFilter.SetLayerMask(enemyLayers);
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
            if (playerAnimationController != null)
            {
                SpriteAnimation attackAnimation = playerAnimationController.PeekNextAttackAnimation();
                string attackAnimName = attackAnimation != null
                    ? attackAnimation.Name
                    : playerAnimationController.PeekNextAttackAnimationName();

                AttackData attackData = attackAnimation != null ? attackAnimation.AttackData as AttackData : null;
                var frameEvents = GetOrBuildFrameEvents(attackAnimName, attackAnimation, attackData);
                playerAnimationController.playerAnim.SetAnimationFrameEvents(attackAnimName, frameEvents);

            }

            playerAnimationController.AttackAnim();
        }

        private Dictionary<int, List<Action>> GetOrBuildFrameEvents(string attackAnimName, SpriteAnimation attackAnimation, AttackData attackData)
        {
            string cacheKey = BuildCacheKey(attackAnimName, attackAnimation, attackData);
            if (cachedFrameEventMaps.TryGetValue(cacheKey, out Dictionary<int, List<Action>> cachedFrameEvents))
                return cachedFrameEvents;

            Dictionary<int, List<Action>> builtFrameEvents = BuildFrameEvents(attackAnimName, attackAnimation, attackData);
            cachedFrameEventMaps[cacheKey] = builtFrameEvents;
            return builtFrameEvents;
        }

        private string BuildCacheKey(string attackAnimName, SpriteAnimation attackAnimation, AttackData attackData)
        {
            int frameCount = attackAnimation != null && attackAnimation.Frames != null ? attackAnimation.Frames.Count : 0;
            int attackDataId = attackData != null ? attackData.GetInstanceID() : 0;
            return attackAnimName + ":" + frameCount + ":" + attackDataId;
        }

        private Dictionary<int, List<Action>> BuildFrameEvents(string attackAnimName, SpriteAnimation attackAnimation, AttackData attackData)
        {
            var frameEvents = new Dictionary<int, List<Action>>();
            bool hasUnlockMovementEvent = false;

            if (attackData != null)
            {
                foreach (FrameEvent frameEvent in attackData.FrameEvents)
                {
                    if (frameEvent == null)
                        continue;

                    int startFrame = ClampFrame(frameEvent.StartFrame, attackAnimation);
                    int endFrame = ClampFrame(frameEvent.EndFrame, attackAnimation);
                    if (endFrame < startFrame)
                        endFrame = startFrame;

                    if (frameEvent.EventType == FrameEventType.UnlockMovement)
                        hasUnlockMovementEvent = true;

                    // Hit windows are visualized as ranges in data, but runtime hit detection is executed once on window entry.
                    if (frameEvent.EventType == FrameEventType.Hit || frameEvent.EventType == FrameEventType.MoveForce)
                    {
                        FrameEvent frameEventCopy = frameEvent;
                        AddFrameEvent(frameEvents, startFrame, () => ExecuteFrameEvent(attackAnimName, attackData, frameEventCopy, startFrame));
                        continue;
                    }

                    for (int frame = startFrame; frame <= endFrame; frame++)
                    {
                        int frameCopy = frame;
                        FrameEvent frameEventCopy = frameEvent;
                        AddFrameEvent(frameEvents, frameCopy, () => ExecuteFrameEvent(attackAnimName, attackData, frameEventCopy, frameCopy));
                    }
                }
            }

            if (!hasUnlockMovementEvent)
            {
                int unlockFrame = ResolveFallbackUnlockFrame(attackAnimation);
                AddFrameEvent(frameEvents, unlockFrame, () => {
                    if (enableDebugLogs)
                        Debug.Log($"[PlayerAttack] Unlock frame event fired for {attackAnimName} at frame {unlockFrame}");
                    playerMovement.StopCancelMoving();
                    playerAnimationController.ForceImmediateStateSync();
                });
            }

            return frameEvents;
        }

        private void AddFrameEvent(Dictionary<int, List<Action>> frameEvents, int frame, Action callback)
        {
            if (!frameEvents.ContainsKey(frame))
                frameEvents[frame] = new List<Action>();

            frameEvents[frame].Add(callback);
        }

        private void ExecuteFrameEvent(string animName, AttackData attackData, FrameEvent frameEvent, int frame)
        {
            switch (frameEvent.EventType)
            {
                case FrameEventType.Hit:
                    OnAttackHit(animName, frame, attackData);
                    break;
                case FrameEventType.UnlockMovement:
                    if (enableDebugLogs)
                        Debug.Log($"[PlayerAttack] UnlockMovement fired for {animName} at frame {frame}");
                    playerMovement.StopCancelMoving();
                    playerAnimationController.ForceImmediateStateSync();
                    break;
                case FrameEventType.MoveForce:
                    ApplyMoveForce(frameEvent, animName, frame);
                    break;
                case FrameEventType.SpawnVFX:
                    if (enableDebugLogs)
                        Debug.Log($"[PlayerAttack] SpawnVFX fired for {animName} at frame {frame}");
                    break;
                case FrameEventType.PlaySound:
                    if (enableDebugLogs)
                        Debug.Log($"[PlayerAttack] PlaySound fired for {animName} at frame {frame}");
                    break;
                case FrameEventType.CameraShake:
                    if (enableDebugLogs)
                        Debug.Log($"[PlayerAttack] CameraShake fired for {animName} at frame {frame}");
                    break;
            }
        }

        private int ResolveFallbackUnlockFrame(SpriteAnimation attackAnimation)
        {
            int fallbackFrame = 4;
            if (attackAnimation == null || attackAnimation.Frames == null || attackAnimation.Frames.Count == 0)
                return fallbackFrame;

            return Mathf.Min(fallbackFrame, attackAnimation.Frames.Count - 1);
        }

        private int ClampFrame(int frame, SpriteAnimation attackAnimation)
        {
            if (attackAnimation == null || attackAnimation.Frames == null || attackAnimation.Frames.Count == 0)
                return Mathf.Max(0, frame);

            return Mathf.Clamp(frame, 0, attackAnimation.Frames.Count - 1);
        }

        // Called when attack hit frame is reached
        private void OnAttackHit(string animName, int frame, AttackData attackData)
        {
            int hitCount = PerformHitDetection(attackData);
            if (enableDebugLogs)
                Debug.Log("<color=yellow>[PlayerAttack]</color> Attack hit event fired for " + animName + " at frame " + frame);
            // Place your hit logic here (e.g., damage enemies)
            if (enableDebugLogs && hitCount > 0)
            {
                Debug.Log($"[PlayerAttack] {hitCount} target(s) found in hitbox.");
            }

            if (hitStopCoroutine == null)
                hitStopCoroutine = StartCoroutine(HitStop());
        }

        private void ApplyMoveForce(FrameEvent frameEvent, string animName, int frame)
        {
            if (rb == null)
                return;

            Vector2 force = frameEvent.MoveForce;
            if (frameEvent.MoveForceUsesFacing)
            {
                float facingSign = Mathf.Approximately(transform.lossyScale.x, 0f) ? 1f : Mathf.Sign(transform.lossyScale.x);
                force.x *= facingSign;
            }

            if (frameEvent.OverrideHorizontalVelocity)
            {
                rb.linearVelocity = new Vector2(force.x, rb.linearVelocity.y + force.y);
            }
            else
            {
                rb.AddForce(force, ForceMode2D.Impulse);
            }

            if (enableDebugLogs)
                Debug.Log($"[PlayerAttack] MoveForce fired for {animName} at frame {frame} with force {force}");
        }

        private int PerformHitDetection(AttackData attackData)
        {
            if (attackData == null || attackData.Hitbox == null)
                return 0;

            EnsureHitDetectionBuffer();

            AttackHitbox hitbox = attackData.Hitbox;
            Vector2 hitboxCenter = GetHitboxCenter(hitbox.Offset);
            int hitCount;

            if (hitbox.Shape == AttackHitboxShape.Box)
            {
                hitCount = Physics2D.OverlapBox(hitboxCenter, hitbox.Size, 0f, hitDetectionFilter, hitDetectionResults);
            }
            else
            {
                hitCount = Physics2D.OverlapCircle(hitboxCenter, hitbox.Radius, hitDetectionFilter, hitDetectionResults);
            }

            if (hitCount >= hitDetectionResults.Length)
            {
                Array.Resize(ref hitDetectionResults, hitDetectionResults.Length * 2);
            }

            return hitCount;
        }

        private void EnsureHitDetectionBuffer()
        {
            if (hitDetectionResults == null || hitDetectionResults.Length == 0)
                hitDetectionResults = new Collider2D[Mathf.Max(4, hitDetectionBufferSize)];
        }

        private Vector2 GetHitboxCenter(Vector2 localOffset)
        {
            float facingSign = Mathf.Approximately(transform.lossyScale.x, 0f) ? 1f : Mathf.Sign(transform.lossyScale.x);
            Vector2 signedOffset = new(localOffset.x * facingSign, localOffset.y);
            return (Vector2)transform.position + signedOffset;
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
            hitStopCoroutine = null;
        }
        #endregion

        #region Debug Area
        private void OnDrawGizmosSelected()
        {
            AttackData attackData = GetPreviewAttackData();
            if (attackData == null || attackData.Hitbox == null)
                return;

            Gizmos.color = Color.red;
            Vector2 center = GetHitboxCenter(attackData.Hitbox.Offset);
            if (attackData.Hitbox.Shape == AttackHitboxShape.Box)
            {
                Gizmos.DrawWireCube(center, attackData.Hitbox.Size);
            }
            else
            {
                Gizmos.DrawWireSphere(center, attackData.Hitbox.Radius);
            }
        }

        private AttackData GetPreviewAttackData()
        {
            if (playerAnimationController == null)
                playerAnimationController = GetComponent<PlayerAnimationController>();

            SpriteAnimation attackAnimation = playerAnimationController != null
                ? playerAnimationController.PeekNextAttackAnimation()
                : null;

            return attackAnimation != null ? attackAnimation.AttackData as AttackData : null;
        }
        #endregion
    }
}
