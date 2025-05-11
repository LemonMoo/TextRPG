using System.Collections.Generic;
using UnityEngine;

// Ensure you have the enum files (PlayerRace.cs, PlayerClass.cs, PlayerOrigin.cs) in your project.

[System.Serializable]
public class Attributes
{
    public int Strength;
    public int Agility;
    public int Intelligence;
    public int Stamina;
    public int Wisdom;     // Drives Mana pool
    public int Fury;       // Drives Rage pool
    public int Endurance;  // Drives Energy pool
    public int Faith;      // Primary stat for Cleric, may influence Mana or other divine mechanics

    // Base constructor for attributes
    public Attributes(int str = 5, int agi = 5, int intel = 5, int sta = 5, int wis = 3, int fur = 3, int end = 3, int fai = 3)
    {
        Strength = str;
        Agility = agi;
        Intelligence = intel;
        Stamina = sta;
        // Set base resource attributes lower by default, they will be boosted by class.
        Wisdom = wis;
        Fury = fur;
        Endurance = end;
        Faith = fai;
    }
}

public class Player : MonoBehaviour
{
    public string PlayerName { get; private set; }
    public PlayerRace Race { get; private set; }
    public PlayerClass Class { get; private set; }
    public PlayerOrigin Origin { get; private set; }
    public Attributes Attributes { get; private set; }

    public int CurrentHealth { get; private set; }
    public int MaxHealth { get; private set; }

    public int CurrentMana { get; private set; }
    public int MaxMana { get; private set; }

    public int CurrentRage { get; private set; }
    public int MaxRage { get; private set; }

    public int CurrentEnergy { get; private set; }
    public int MaxEnergy { get; private set; }

    public List<string> Inventory { get; private set; }

    public int Level { get; private set; }
    public int CurrentExperience { get; private set; }
    public int ExperienceToNextLevel { get; private set; }

    // Constants for how attributes affect resources
    private const int HEALTH_PER_STAMINA_POINT = 10;
    private const int MANA_PER_WISDOM_POINT = 10;    // Wisdom directly fuels Mana
    private const int RAGE_PER_FURY_POINT = 10;      // Fury directly fuels Rage
    private const int ENERGY_PER_ENDURANCE_POINT = 10; // Endurance directly fuels Energy

    private bool isInitialized = false;

    public void InitializePlayer(string playerName, PlayerRace race, PlayerClass playerClass, PlayerOrigin origin, Attributes baseAttributes)
    {
        if (isInitialized)
        {
            Debug.LogWarning("Player already initialized.");
            return;
        }

        PlayerName = playerName;
        Race = race;
        Class = playerClass;
        Origin = origin;
        // Attributes are passed in (likely from new Attributes() in CharacterCreationUI)
        // then specialized by ApplyAttributeModifiers.
        Attributes = baseAttributes;

        ApplyAttributeModifiers(); // Specialize attributes based on class

        Inventory = new List<string>();
        Level = 1;
        CurrentExperience = 0;
        ExperienceToNextLevel = 100;

        CalculateDerivedStats();
        RestoreToMaxStats();

        isInitialized = true;
        Debug.Log($"Player '{PlayerName}' the {Race} {Class} from {Origin} has been initialized with specialized attributes.");
    }

    private void ApplyAttributeModifiers()
    {
        if (Attributes == null) Attributes = new Attributes(); // Should not happen if initialized correctly

        // --- Attribute Specialization by Class ---
        // 1. Set all primary resource attributes to a low baseline.
        //    This ensures only the class-relevant ones will be high.
        //    Adjust these base values as needed for balance (e.g., if you want some minimal off-class resource).
        int baseResourceStat = 3; // A minimal value for non-primary resource stats
        Attributes.Wisdom = baseResourceStat;     // For Mana
        Attributes.Fury = baseResourceStat;       // For Rage
        Attributes.Endurance = baseResourceStat;  // For Energy
        // Faith is also a primary stat, so it's handled per class. If not Cleric, it can be low.
        Attributes.Faith = baseResourceStat;


        // 2. Boost attributes based on the selected class.
        //    These are ADDITIVE to the base values set above or the default constructor values for non-resource stats.
        //    The values here are significant boosts for primary stats.
        int primaryStatBoost = 7; // e.g., total of 10 (3 base + 7 boost)
        int secondaryStatBoost = 4; // e.g., total of 7-9

        switch (Class)
        {
            case PlayerClass.Fighter:
                Attributes.Strength += primaryStatBoost; // Main damage stat
                Attributes.Fury += primaryStatBoost;     // Main resource stat
                Attributes.Stamina += secondaryStatBoost;  // More health
                break;

            case PlayerClass.Wizard:
                Attributes.Intelligence += primaryStatBoost; // Main damage stat
                Attributes.Wisdom += primaryStatBoost;       // Main resource stat (Mana)
                break;

            case PlayerClass.Scout:
                Attributes.Agility += primaryStatBoost;    // Main damage/utility stat
                Attributes.Endurance += primaryStatBoost;  // Main resource stat (Energy)
                // Scouts might also get a perception boost if that attribute is added
                break;

            case PlayerClass.Ranger:
                Attributes.Agility += primaryStatBoost;    // Main damage/utility stat
                Attributes.Wisdom += secondaryStatBoost;   // Secondary resource stat (Mana for spells)
                // Rangers might also get a perception boost
                break;

            case PlayerClass.Cleric:
                Attributes.Faith += primaryStatBoost;      // Main spell power stat
                Attributes.Wisdom += primaryStatBoost;     // Main resource stat (Mana)
                Attributes.Stamina += secondaryStatBoost;  // More survivability
                break;
        }

        // 3. (Optional) Apply racial modifiers AFTER class specialization
        // switch (Race)
        // {
        //    case PlayerRace.Dwarf: Attributes.Stamina += 2; Attributes.Strength += 1; break;
        //    case PlayerRace.Elf: Attributes.Agility += 1; Attributes.Intelligence += 1; break;
        //    // ... etc.
        // }
        Debug.Log($"Attributes applied for {Class}. Wisdom: {Attributes.Wisdom}, Fury: {Attributes.Fury}, Endurance: {Attributes.Endurance}, Faith: {Attributes.Faith}");
    }

    public void CalculateDerivedStats()
    {
        if (Attributes == null)
        {
            Debug.LogError("Attributes not set before calculating derived stats!");
            Attributes = new Attributes();
        }
        MaxHealth = Attributes.Stamina * HEALTH_PER_STAMINA_POINT;
        MaxMana = Attributes.Wisdom * MANA_PER_WISDOM_POINT;
        MaxRage = Attributes.Fury * RAGE_PER_FURY_POINT;
        MaxEnergy = Attributes.Endurance * ENERGY_PER_ENDURANCE_POINT;
    }

    public void RestoreToMaxStats()
    {
        CurrentHealth = MaxHealth;
        CurrentMana = MaxMana;
        CurrentRage = MaxRage;
        CurrentEnergy = MaxEnergy;
    }

    public void TakeDamage(int amount)
    {
        if (!isInitialized) return;
        CurrentHealth -= amount;
        if (CurrentHealth < 0) CurrentHealth = 0;
        if (CurrentHealth <= 0) Debug.Log($"{PlayerName} has been defeated!");
    }

    public void Heal(int amount)
    {
        if (!isInitialized) return;
        CurrentHealth += amount;
        if (CurrentHealth > MaxHealth) CurrentHealth = MaxHealth;
    }

    public void GainExperience(int amount)
    {
        if (!isInitialized) return;
        CurrentExperience += amount;
        Debug.Log($"{PlayerName} gained {amount} experience.");
        CheckForLevelUp();
    }

    private void CheckForLevelUp()
    {
        while (CurrentExperience >= ExperienceToNextLevel)
        {
            Level++;
            CurrentExperience -= ExperienceToNextLevel;
            ExperienceToNextLevel = Mathf.RoundToInt(ExperienceToNextLevel * 1.5f);

            // Universal attribute gain on level up (can be specialized too)
            Attributes.Strength++;
            Attributes.Agility++;
            Attributes.Intelligence++;
            Attributes.Stamina++;
            Attributes.Wisdom++;
            Attributes.Fury++;
            Attributes.Endurance++;
            Attributes.Faith++;

            // If a stat was set to a very low base (e.g. 1) and the class doesn't use it,
            // it will still increment. You might want to refine level-up gains
            // to only boost stats relevant to the class or give players points to spend.
            // For now, simple increment for all.

            CalculateDerivedStats();
            RestoreToMaxStats();
            Debug.Log($"{PlayerName} reached Level {Level}! Next level at {ExperienceToNextLevel} EXP. All attributes increased by 1.");
        }
    }

    public bool UseMana(int amount)
    {
        if (!isInitialized || CurrentMana < amount)
        {
            if (isInitialized && MaxMana > 0) Debug.Log("Not enough mana!");
            else if (isInitialized && MaxMana == 0) Debug.Log("You do not have a mana pool.");
            return false;
        }
        CurrentMana -= amount;
        return true;
    }

    public bool UseRage(int amount)
    {
        if (!isInitialized || CurrentRage < amount)
        {
            if (isInitialized && MaxRage > 0) Debug.Log("Not enough rage!");
            else if (isInitialized && MaxRage == 0) Debug.Log("You do not have a rage pool.");
            return false;
        }
        CurrentRage -= amount;
        return true;
    }

    public bool UseEnergy(int amount)
    {
        if (!isInitialized || CurrentEnergy < amount)
        {
            if (isInitialized && MaxEnergy > 0) Debug.Log("Not enough energy!");
            else if (isInitialized && MaxEnergy == 0) Debug.Log("You do not have an energy pool.");
            return false;
        }
        CurrentEnergy -= amount;
        return true;
    }

    public void AddItemToInventory(string itemName)
    {
        if (!isInitialized) return;
        Inventory.Add(itemName);
        Debug.Log($"{itemName} added to inventory.");
    }

    public bool RemoveItemFromInventory(string itemName)
    {
        if (!isInitialized) return false;
        bool removed = Inventory.Remove(itemName);
        if (removed) Debug.Log($"{itemName} removed from inventory.");
        else Debug.Log($"{itemName} not found in inventory.");
        return removed;
    }

    public string GetPlayerStats()
    {
        if (!isInitialized) return "Player data not available yet.";

        System.Text.StringBuilder statsBuilder = new System.Text.StringBuilder();
        statsBuilder.AppendLine("--- Player Stats ---");
        statsBuilder.AppendLine($"Name: {PlayerName}");
        statsBuilder.AppendLine($"Race: {Race}");
        statsBuilder.AppendLine($"Class: {Class}");
        statsBuilder.AppendLine($"Origin: {Origin}");
        statsBuilder.AppendLine($"Level: {Level}");
        statsBuilder.AppendLine($"Experience: {CurrentExperience} / {ExperienceToNextLevel}");
        statsBuilder.AppendLine("--------------------");
        statsBuilder.AppendLine($"Health: {CurrentHealth} / {MaxHealth}");
        statsBuilder.AppendLine($"Mana:   {CurrentMana} / {MaxMana} (from Wisdom: {Attributes.Wisdom})");
        statsBuilder.AppendLine($"Rage:   {CurrentRage} / {MaxRage} (from Fury: {Attributes.Fury})");
        statsBuilder.AppendLine($"Energy: {CurrentEnergy} / {MaxEnergy} (from Endurance: {Attributes.Endurance})");
        statsBuilder.AppendLine("--------------------");
        statsBuilder.AppendLine("Attributes:");
        statsBuilder.AppendLine($"  Strength:     {Attributes.Strength}");
        statsBuilder.AppendLine($"  Agility:      {Attributes.Agility}");
        statsBuilder.AppendLine($"  Intelligence: {Attributes.Intelligence}");
        statsBuilder.AppendLine($"  Stamina:      {Attributes.Stamina}");
        statsBuilder.AppendLine($"  Wisdom:       {Attributes.Wisdom}");
        statsBuilder.AppendLine($"  Fury:         {Attributes.Fury}");
        statsBuilder.AppendLine($"  Endurance:    {Attributes.Endurance}");
        statsBuilder.AppendLine($"  Faith:        {Attributes.Faith}");
        statsBuilder.AppendLine("--------------------");
        return statsBuilder.ToString();
    }

    public string GetInventoryList()
    {
        if (!isInitialized) return "Player data not available yet.";
        if (Inventory.Count == 0)
        {
            return "Inventory is empty.";
        }
        System.Text.StringBuilder invResult = new System.Text.StringBuilder("Inventory:\n");
        foreach (string item in Inventory)
        {
            invResult.AppendLine("- " + item);
        }
        return invResult.ToString();
    }
}
