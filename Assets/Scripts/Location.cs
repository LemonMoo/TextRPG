// File: Location.cs
using System.Collections.Generic;
using UnityEngine;

// Assuming Item.cs and Enemy.cs (with EnemyType enum) are defined and in your project.

// --- Item Templates (Blueprints for items enemies can drop) ---
// It's often better to have a dedicated ItemDatabase script/asset for a larger game,
// but for simplicity, we can define some templates here or have CreateEnemyFromType make them.
public static class ItemTemplates
{
    // Generic Loot/Crafting Materials
    public static readonly Item GoblinEar = new Item("Goblin Ear", "A grimy goblin ear. Proof of a kill, or perhaps an ingredient?", ItemType.Generic, 2, true);
    public static readonly Item WolfPelt = new Item("Wolf Pelt", "A rough wolf pelt. Could be useful for crafting or trade.", ItemType.Generic, 5, true);
    public static readonly Item SpiderSilk = new Item("Spider Silk", "A sticky strand of potent spider silk.", ItemType.Generic, 3, true);
    public static readonly Item BanditMask = new Item("Bandit Mask", "A tattered mask, often worn by highwaymen.", ItemType.Generic, 10, false);

    // Consumables
    public static readonly Item MinorHealingPotion = new Item("Minor Healing Potion", "A common potion that restores a small amount of health.", ItemType.Potion, 25, true);
    public static readonly Item CrustyBread = new Item("Crusty Bread", "A somewhat stale loaf of bread. Better than nothing.", ItemType.Generic, 1, true); // Generic could be food type
}


[System.Serializable]
public class Location
{
    public string LocationID;
    public string Name;
    [TextArea(3, 10)]
    public string Description;

    public Dictionary<string, string> Exits;

    // --- Enemy Encounter Data ---
    public List<EnemyType> PossibleEnemyTypes;
    public float EncounterChance;
    public int MaxEnemiesInLocation;

    public Location(string id, string name, string description, float encounterChance = 0f, int maxEnemies = 1)
    {
        LocationID = id;
        Name = name;
        Description = description;
        Exits = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);

        PossibleEnemyTypes = new List<EnemyType>();
        EncounterChance = Mathf.Clamp01(encounterChance); // Ensure chance is between 0 and 1
        MaxEnemiesInLocation = Mathf.Max(1, maxEnemies);
    }

    public void AddExit(string direction, string targetLocationID)
    {
        if (!Exits.ContainsKey(direction.ToLower()))
        {
            Exits.Add(direction.ToLower(), targetLocationID);
        }
        else
        {
            Debug.LogWarning($"Exit '{direction}' already exists for location '{LocationID}'.", null);
        }
    }

    public void AddPossibleEnemy(EnemyType enemyType)
    {
        if (!PossibleEnemyTypes.Contains(enemyType))
        {
            PossibleEnemyTypes.Add(enemyType);
        }
    }

    public string GetExitsDescription()
    {
        if (Exits.Count == 0)
        {
            return "There are no obvious exits.";
        }

        System.Text.StringBuilder exitDesc = new System.Text.StringBuilder("You can go: ");
        List<string> exitDirections = new List<string>(Exits.Keys);
        for (int i = 0; i < exitDirections.Count; i++)
        {
            exitDesc.Append(exitDirections[i]);
            if (i < exitDirections.Count - 1)
            {
                exitDesc.Append(", ");
            }
        }
        exitDesc.Append(".");
        return exitDesc.ToString();
    }

    public List<Enemy> GetEncounterPack()
    {
        List<Enemy> encounterPack = new List<Enemy>();
        // Debug.Log($"Location ({Name}): GetEncounterPack. PossibleTypes: {PossibleEnemyTypes.Count}, Chance: {EncounterChance}", null);

        if (PossibleEnemyTypes.Count == 0 || EncounterChance <= 0)
        {
            // Debug.Log($"Location ({Name}): No possible enemies or zero encounter chance.", null);
            return encounterPack;
        }

        float randomRoll = Random.Range(0f, 1f);
        // Debug.Log($"Location ({Name}): Rolled {randomRoll} against Chance {EncounterChance}.", null);

        if (randomRoll > EncounterChance) // If roll is GREATER than chance, NO encounter
        {
            // Debug.Log($"Location ({Name}): Encounter chance failed.", null);
            return encounterPack;
        }

        int numberOfEnemies = Random.Range(1, MaxEnemiesInLocation + 1);
        // Debug.Log($"Location ({Name}): Encounter SUCCESS! Generating {numberOfEnemies} enemies.", null);

        for (int i = 0; i < numberOfEnemies; i++)
        {
            EnemyType randomType = PossibleEnemyTypes[Random.Range(0, PossibleEnemyTypes.Count)];
            Enemy newEnemy = CreateEnemyFromType(randomType);
            if (newEnemy != null)
            {
                encounterPack.Add(newEnemy);
            }
            else
            {
                Debug.LogWarning($"Location ({Name}): Failed to create enemy instance for type: {randomType}.", null);
            }
        }
        return encounterPack;
    }

    // This method defines the stats, gold, and loot for each enemy type.
    // You will expand this as you add more enemy types and items.
    private Enemy CreateEnemyFromType(EnemyType type)
    {
        Enemy newEnemy = null;
        // Attributes constructor: (str, agi, intel, sta, wis, fur, end, fai) - ensure order matches your Attributes.cs
        // Enemy constructor: (name, type, stats, minDmg, maxDmg, xp, minGold, maxGold)

        switch (type)
        {
            case EnemyType.Goblin:
                // Goblins: Low health, quick, decent numbers.
                // Gold: 1-5. Loot: Goblin Ears, sometimes basic supplies.
                newEnemy = new Enemy("Goblin Scavenger", type,
                                    new Attributes(str: 6, agi: 7, intel: 3, sta: 4, wis: 2, fur: 4, end: 4, fai: 1),
                                    2, 4, 10, // minDmg, maxDmg, xp
                                    1, 5);   // minGold, maxGold
                newEnemy.AddLoot(ItemTemplates.GoblinEar, 0.6f, 1, 2);  // 60% chance, 1-2 ears
                newEnemy.AddLoot(ItemTemplates.CrustyBread, 0.15f);     // 15% chance for bread
                // Debug.Log($"Creating Goblin. Gold: {newEnemy.MinGoldDrop}-{newEnemy.MaxGoldDrop}, Loot Count: {newEnemy.PotentialLoot.Count}", null);
                break;

            case EnemyType.Wolf:
                // Wolves: Moderate health, good damage, often in packs.
                // Gold: 3-10. Loot: Wolf Pelts.
                newEnemy = new Enemy("Forest Wolf", type,
                                    new Attributes(str: 7, agi: 8, intel: 2, sta: 6, wis: 2, fur: 5, end: 6, fai: 1),
                                    3, 6, 15, // minDmg, maxDmg, xp
                                    3, 10);  // minGold, maxGold
                newEnemy.AddLoot(ItemTemplates.WolfPelt, 0.70f);        // 70% chance for a pelt
                // Debug.Log($"Creating Wolf. Gold: {newEnemy.MinGoldDrop}-{newEnemy.MaxGoldDrop}, Loot Count: {newEnemy.PotentialLoot.Count}", null);
                break;

            case EnemyType.ForestSpider:
                // Spiders: Can be tough, maybe poison later.
                // Gold: 2-7. Loot: Spider Silk.
                newEnemy = new Enemy("Giant Forest Spider", type,
                                    new Attributes(str: 5, agi: 9, intel: 2, sta: 5, wis: 1, fur: 3, end: 4, fai: 1),
                                    2, 5, 12, // minDmg, maxDmg, xp
                                    2, 7);   // minGold, maxGold
                newEnemy.AddLoot(ItemTemplates.SpiderSilk, 0.5f, 1, 3); // 50% chance for 1-3 silk
                // Debug.Log($"Creating Spider. Gold: {newEnemy.MinGoldDrop}-{newEnemy.MaxGoldDrop}, Loot Count: {newEnemy.PotentialLoot.Count}", null);
                break;

            case EnemyType.Bandit:
                // Bandits: Humanoid, can be more varied. Higher gold potential.
                // Gold: 10-25. Loot: Bandit Masks, sometimes potions or more valuable vendor trash.
                newEnemy = new Enemy("Road Bandit", type,
                                    new Attributes(str: 8, agi: 7, intel: 5, sta: 7, wis: 4, fur: 5, end: 5, fai: 3),
                                    4, 7, 20,  // minDmg, maxDmg, xp
                                    10, 25);   // minGold, maxGold
                newEnemy.AddLoot(ItemTemplates.BanditMask, 0.20f);          // 20% chance for mask
                newEnemy.AddLoot(ItemTemplates.MinorHealingPotion, 0.10f);  // 10% chance for potion
                // Debug.Log($"Creating Bandit. Gold: {newEnemy.MinGoldDrop}-{newEnemy.MaxGoldDrop}, Loot Count: {newEnemy.PotentialLoot.Count}", null);
                break;

            default:
                Debug.LogWarning($"Location ({this.Name}): CreateEnemyFromType - No definition for enemy type: {type}. Creating a generic placeholder.", null);
                // Create a very basic placeholder enemy if type is unknown
                newEnemy = new Enemy("Unknown Creature", type,
                                    new Attributes(str: 5, agi: 5, intel: 5, sta: 5, wis: 3, fur: 3, end: 3, fai: 3),
                                    1, 3, 5, 0, 1); // Minimal stats, xp, gold
                break;
        }
        return newEnemy;
    }
}