using UnityEngine;
using System.Collections.Generic; // For list of abilities later

public enum EnemyType // Example types
{
    Goblin,
    Wolf,
    ForestSpider,
    Bandit
}

[System.Serializable]
public class Enemy
{
    public string Name;
    public EnemyType Type;
    public Attributes Stats; // Enemies will also use the Attributes system

    public int CurrentHealth;
    public int MaxHealth;

    // Basic combat values (can be derived from stats or set directly)
    public int MinDamage;
    public int MaxDamage;

    // Future: List of abilities/spells
    // public List<string> Abilities; 

    public int ExperienceReward; // EXP given to player on defeat

    public Enemy(string name, EnemyType type, Attributes stats, int minDamage, int maxDamage, int experienceReward)
    {
        Name = name;
        Type = type;
        Stats = stats; // Make sure to provide an Attributes object

        // Calculate MaxHealth from enemy's Stamina
        MaxHealth = Stats.Stamina * 10; // Using same formula as player for consistency
        CurrentHealth = MaxHealth;

        MinDamage = minDamage;
        MaxDamage = maxDamage;
        ExperienceReward = experienceReward;
        // Abilities = new List<string>();
    }

    public void TakeDamage(int amount)
    {
        CurrentHealth -= amount;
        if (CurrentHealth < 0)
        {
            CurrentHealth = 0;
        }
        Debug.Log($"{Name} takes {amount} damage. Health: {CurrentHealth}/{MaxHealth}");
    }

    public bool IsDefeated()
    {
        return CurrentHealth <= 0;
    }

    // Basic attack action for the enemy
    public int PerformAttack()
    {
        int damage = Random.Range(MinDamage, MaxDamage + 1);
        // Future: Could be modified by enemy's Strength or other stats
        // damage += Stats.Strength / 2; 
        Debug.Log($"{Name} attacks for {damage} damage!");
        return damage;
    }
}
