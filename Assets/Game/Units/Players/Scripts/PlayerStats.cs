using UnityEngine;

namespace Game.Units.Players
{
    // Handles all player stats (health, stamina, attack, defense, etc.)
    public class PlayerStats : MonoBehaviour
    {
        [Header("Base Stats")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int attack = 10;
    // [SerializeField] private int defense = 5;
    // [SerializeField] private float moveSpeed = 8f;
    // [SerializeField] private float critChance = 0.05f;
    // [SerializeField] private float critMultiplier = 2f;

    public int MaxHealth => maxHealth;
    public int Attack => attack;
    // public int Defense => defense;
    // public float MoveSpeed => moveSpeed;
    // public float CritChance => critChance;
    // public float CritMultiplier => critMultiplier;
        //? Add more stats as needed

        // Example: stat modifiers (buffs/debuffs)
        // public List<StatModifier> statModifiers;

        // You can add methods to modify stats, apply buffs, etc.
    }
}
