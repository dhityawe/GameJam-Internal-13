using UnityEngine;
using System;

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
            playerAnimationController.AttackAnim();
            playerMovement.CancelMoving();

            if (attackPoint == null)
            {
                Debug.LogWarning("[PlayerAttack] AttackPoint not assigned!");
                return;
            }

            // Detect enemies in range
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);
            foreach (var enemy in hitEnemies)
            {
                //? Try to call TakeDamage on the enemy
                // var damageable = enemy.GetComponent<IDamageable>();
                // if (damageable != null)
                // {
                //     damageable.TakeDamage(1); // You can adjust damage value as needed
                // }
                // else
                // {
                //     // Fallback: look for a method named TakeDamage(int)
                //     var method = enemy.GetType().GetMethod("TakeDamage", new Type[] { typeof(int) });
                //     if (method != null)
                //     {
                //         method.Invoke(enemy, new object[] { 1 });
                //     }
                // }
            }
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
        private void OnDrawGizmosSelected()
        {
            if (attackPoint != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(attackPoint.position, attackRange);
            }
        }
    }
}
