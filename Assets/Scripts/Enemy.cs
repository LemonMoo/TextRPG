// File: Enemy.cs
using UnityEngine;
using System.Collections.Generic; // For List

// EnemyType enum should be defined elsewhere or here if not already

[System.Serializable]
public class LootDrop
{
    public Item ItemToDrop; // This will be a reference to an Item object or a template
    public float DropChance; // 0.0 to 1.0
    public int MinQuantity;  // Min quantity if item is stackable and drops
    public int MaxQuantity;  // Max quantity if item is stackable and drops

    public LootDrop(Item item, float chance, int minQty = 1, int maxQty = 1)
    {
        ItemToDrop = item;
        DropChance = Mathf.Clamp01(chance);
        MinQuantity = Mathf.Max(1, minQty);
        MaxQuantity = Mathf.Max(MinQuantity, maxQty);
    }
}

[System.Serializable]
public class Enemy
{
    public string Name;
    public EnemyType Type;
    public Attributes Stats;    
    public int Level { get; private set; }

    public int CurrentHealth;
    public int MaxHealth;

    public int MinDamage;
    public int MaxDamage;

    public int ExperienceReward;

    // --- NEW: Loot and Gold ---
    public int MinGoldDrop;
    public int MaxGoldDrop;
    public List<LootDrop> PotentialLoot; // List of items this enemy can drop

    public Enemy(string name, EnemyType type, Attributes stats, int minDamage, int maxDamage, int experienceReward,
                 int minGold = 0, int maxGold = 0, int level = 1) // Added gold params
    {
        Name = name;
        Type = type;
        Stats = stats;
        Level = level;

        MaxHealth = Stats.Stamina * 10; // Assuming 10 is your health per stamina point
        CurrentHealth = MaxHealth;

        MinDamage = minDamage;
        MaxDamage = maxDamage;
        ExperienceReward = experienceReward;

        MinGoldDrop = minGold;
        MaxGoldDrop = Mathf.Max(minGold, maxGold); // Ensure max is not less than min
        PotentialLoot = new List<LootDrop>();

        // Debug.Log($"Enemy Created: {Name}, HP: {CurrentHealth}/{MaxHealth}, Gold: {MinGoldDrop}-{MaxGoldDrop}", null);
    }

    // Method to add a potential loot item to this enemy type
    public void AddLoot(Item item, float chance, int minQty = 1, int maxQty = 1)
    {
        if (item != null)
        {
            PotentialLoot.Add(new LootDrop(item, chance, minQty, maxQty));
        }
    }

    public void TakeDamage(int amount)
    {
        CurrentHealth -= amount;
        // Debug.Log($"{Name} takes {amount} damage. Health: {CurrentHealth}/{MaxHealth}", null);
        if (CurrentHealth < 0) CurrentHealth = 0;
    }

    public bool IsDefeated()
    {
        return CurrentHealth <= 0;
    }

    public int PerformAttack()
    {
        int damage = Random.Range(MinDamage, MaxDamage + 1);
        // LogToCombatOrGameConsole in GameManager will announce this attack
        return damage; // Return damage, GameManager will log it
    }

    // --- NEW: Method to get actual dropped loot ---
    public List<Item> GetDroppedLoot(out int goldDropped)
    {
        List<Item> droppedItems = new List<Item>();
        goldDropped = Random.Range(MinGoldDrop, MaxGoldDrop + 1);

        foreach (LootDrop lootEntry in PotentialLoot)
        {
            if (Random.Range(0f, 1f) <= lootEntry.DropChance)
            {
                int quantityToDrop = 1;
                if (lootEntry.ItemToDrop.IsStackable)
                {
                    quantityToDrop = Random.Range(lootEntry.MinQuantity, lootEntry.MaxQuantity + 1);
                }
                // Create a NEW instance of the item for the drop
                Item newItemInstance = new Item(lootEntry.ItemToDrop.Name,
                                                lootEntry.ItemToDrop.Description,
                                                lootEntry.ItemToDrop.Type,
                                                lootEntry.ItemToDrop.GoldValue,
                                                lootEntry.ItemToDrop.IsStackable,
                                                quantityToDrop);
                droppedItems.Add(newItemInstance);
            }
        }
        return droppedItems;
    }
}