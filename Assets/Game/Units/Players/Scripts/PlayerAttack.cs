using UnityEngine;

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
        private void Awake()
        {
            playerStats = GetComponent<PlayerStats>();
        }

        void Start()
        {
            playerAnimationController = GetComponent<PlayerAnimationController>();
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
            // Example: deal damage using playerStats.attack
            // Play attack animation, spawn hitbox, etc.

        }
    }
}
