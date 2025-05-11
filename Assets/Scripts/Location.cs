using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Location
{
    public string LocationID;
    public string Name;
    [TextArea(3, 10)]
    public string Description;

    public Dictionary<string, string> Exits;

    // --- Enemy Encounter Data ---
    public List<EnemyType> PossibleEnemyTypes; // List of enemy types that can spawn here
    public float EncounterChance; // Chance (0.0 to 1.0) to encounter an enemy on entering this location
    public int MaxEnemiesInLocation; // Max number of enemies for a potential encounter pack

    public Location(string id, string name, string description, float encounterChance = 0f, int maxEnemies = 1)
    {
        LocationID = id;
        Name = name;
        Description = description;
        Exits = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);

        PossibleEnemyTypes = new List<EnemyType>();
        EncounterChance = encounterChance;
        MaxEnemiesInLocation = Mathf.Max(1, maxEnemies); // Ensure at least 1 if encounters are possible
    }

    public void AddExit(string direction, string targetLocationID)
    {
        if (!Exits.ContainsKey(direction.ToLower()))
        {
            Exits.Add(direction.ToLower(), targetLocationID);
        }
        else
        {
            Debug.LogWarning($"Exit '{direction}' already exists for location '{LocationID}'.");
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

    // Method to get a list of enemies for an encounter (implementation detail for CombatManager)
    public List<Enemy> GetEncounterPack()
    {
        List<Enemy> encounterPack = new List<Enemy>();
        if (PossibleEnemyTypes.Count == 0 || Random.Range(0f, 1f) > EncounterChance)
        {
            return encounterPack; // No encounter or failed chance
        }

        int numberOfEnemies = Random.Range(1, MaxEnemiesInLocation + 1);
        for (int i = 0; i < numberOfEnemies; i++)
        {
            if (PossibleEnemyTypes.Count > 0)
            {
                EnemyType randomType = PossibleEnemyTypes[Random.Range(0, PossibleEnemyTypes.Count)];
                // We need a factory or a way to create actual Enemy instances from EnemyType
                Enemy newEnemy = CreateEnemyFromType(randomType);
                if (newEnemy != null)
                {
                    encounterPack.Add(newEnemy);
                }
            }
        }
        return encounterPack;
    }

    // Helper method to create Enemy instances - This would ideally be in an EnemyFactory or GameManager
    // For now, basic implementation here.
    private Enemy CreateEnemyFromType(EnemyType type)
    {
        // Define base stats for different enemy types.
        // These Attributes objects should be created with appropriate values for each enemy.
        switch (type)
        {
            case EnemyType.Goblin:
                // Goblins: Low Stamina, moderate Strength/Agility
                return new Enemy("Goblin Scavenger", type, new Attributes(str: 6, agi: 7, intel: 3, sta: 4, wis: 2, fur: 4, end: 4, fai: 1), 2, 4, 10);
            case EnemyType.Wolf:
                // Wolves: Good Agility/Endurance, decent Strength
                return new Enemy("Forest Wolf", type, new Attributes(str: 7, agi: 8, intel: 2, sta: 6, wis: 2, fur: 5, end: 6, fai: 1), 3, 6, 15);
            case EnemyType.ForestSpider:
                // Spiders: High Agility, low other stats, maybe poison? (future)
                return new Enemy("Giant Forest Spider", type, new Attributes(str: 5, agi: 9, intel: 2, sta: 5, wis: 1, fur: 3, end: 4, fai: 1), 2, 5, 12);
            case EnemyType.Bandit:
                // Bandits: Balanced human-like stats
                return new Enemy("Road Bandit", type, new Attributes(str: 8, agi: 7, intel: 5, sta: 7, wis: 4, fur: 5, end: 5, fai: 3), 4, 7, 20);
            default:
                Debug.LogWarning($"No definition for creating enemy of type: {type}");
                return null;
        }
    }
}
