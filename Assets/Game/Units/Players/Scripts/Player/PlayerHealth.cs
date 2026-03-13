using UnityEngine;
using System;

namespace Game.Units.Players
{
    // Handles health logic: damage, healing, death, invulnerability
    public class PlayerHealth : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField] private int currentHealth;
        private PlayerStats playerStats;

        public static Action OnDeath;
        public static Action<int> OnHealthChanged;

        public int CurrentHealth => currentHealth;

        private void Awake()
        {
            playerStats = GetComponent<PlayerStats>();
            if (playerStats != null)
                currentHealth = playerStats.MaxHealth;
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0) return;
            currentHealth -= amount;
            OnHealthChanged?.Invoke(currentHealth);
            if (currentHealth <= 0)
            {
                currentHealth = 0;
                Die();
            }
        }

        public void Heal(int amount)
        {
            if (amount <= 0) return;
            currentHealth += amount;
            if (playerStats != null)
                currentHealth = Mathf.Min(currentHealth, playerStats.MaxHealth);
            OnHealthChanged?.Invoke(currentHealth);
        }

        private void Die()
        {
            OnDeath?.Invoke();
            // Add death logic (animation, respawn, etc.)
        }
    }
}
