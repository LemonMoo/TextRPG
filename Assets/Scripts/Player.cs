// File: Player.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// PlayerRace, PlayerClass, PlayerOrigin, Attributes, Item enums/classes should be defined

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

    public List<Item> PlayerInventory { get; private set; }
    public int Gold { get; private set; }

    public int Level { get; private set; }
    public int CurrentExperience { get; private set; }
    public int ExperienceToNextLevel { get; private set; }

    private const int HEALTH_PER_STAMINA_POINT = 10;
    private const int MANA_PER_WISDOM_POINT = 10;
    private const int RAGE_PER_FURY_POINT = 10;
    private const int ENERGY_PER_ENDURANCE_POINT = 10;

    private bool isInitializedInternal = false; // Renamed internal flag
    public bool IsInitialized => isInitializedInternal; // Public getter

    public void InitializePlayer(string playerName, PlayerRace race, PlayerClass playerClass, PlayerOrigin origin, Attributes baseAttributes)
    {
        if (isInitializedInternal)
        {
            Debug.LogWarning("Player already initialized.", this);
            return;
        }

        PlayerName = playerName;
        Race = race;
        Class = playerClass;
        Origin = origin;
        Attributes = baseAttributes;

        ApplyAttributeModifiers();
        CalculateDerivedStats();
        RestoreToMaxStats();

        PlayerInventory = new List<Item>();
        Gold = 0; // Or a starting gold amount

        Level = 1;
        CurrentExperience = 0;
        ExperienceToNextLevel = 100;

        isInitializedInternal = true; // Set the flag
        Debug.Log($"Player '{PlayerName}' initialized. Class: {Class}, HP: {CurrentHealth}/{MaxHealth}, Gold: {Gold}. IsInitialized: {IsInitialized}", this);
    }

    private void ApplyAttributeModifiers()
    {
        if (Attributes == null) Attributes = new Attributes();

        int baseResourceStat = 3;
        Attributes.Wisdom = baseResourceStat;
        Attributes.Fury = baseResourceStat;
        Attributes.Endurance = baseResourceStat;
        Attributes.Faith = baseResourceStat;

        int primaryStatBoost = 7;
        int secondaryStatBoost = 4;

        switch (Class)
        {
            case PlayerClass.Fighter:
                Attributes.Strength += primaryStatBoost;
                Attributes.Fury += primaryStatBoost;
                Attributes.Stamina += secondaryStatBoost;
                break;
            case PlayerClass.Wizard:
                Attributes.Intelligence += primaryStatBoost;
                Attributes.Wisdom += primaryStatBoost;
                break;
            case PlayerClass.Scout:
                Attributes.Agility += primaryStatBoost;
                Attributes.Endurance += primaryStatBoost;
                break;
            case PlayerClass.Ranger:
                Attributes.Agility += primaryStatBoost; // Main stat
                Attributes.Wisdom += secondaryStatBoost;  // For mana pool for spells
                break;
            case PlayerClass.Cleric:
                Attributes.Faith += primaryStatBoost;    // Main spell power stat
                Attributes.Wisdom += primaryStatBoost;   // Main resource pool (Mana)
                Attributes.Stamina += secondaryStatBoost;
                break;
        }
        // Debug.Log($"Attributes applied for {Class}. STR:{Attributes.Strength} AGI:{Attributes.Agility} INT:{Attributes.Intelligence} STA:{Attributes.Stamina} WIS:{Attributes.Wisdom} FUR:{Attributes.Fury} END:{Attributes.Endurance} FAI:{Attributes.Faith}", this);
    }

    public void CalculateDerivedStats()
    {
        if (Attributes == null) { Debug.LogError("Attributes not set!", this); Attributes = new Attributes(); }
        MaxHealth = Attributes.Stamina * HEALTH_PER_STAMINA_POINT;
        MaxMana = Attributes.Wisdom * MANA_PER_WISDOM_POINT;
        MaxRage = Attributes.Fury * RAGE_PER_FURY_POINT;
        MaxEnergy = Attributes.Endurance * ENERGY_PER_ENDURANCE_POINT;
        // Debug.Log($"Player.CalculateDerivedStats: Stamina: {Attributes.Stamina}, MaxHealth: {MaxHealth}", this);
    }

    public void RestoreToMaxStats()
    {
        CurrentHealth = MaxHealth;
        CurrentMana = MaxMana;
        CurrentRage = MaxRage;
        CurrentEnergy = MaxEnergy;
        // Debug.Log($"Player.RestoreToMaxStats: CurrentHealth: {CurrentHealth}", this);
    }

    public void TakeDamage(int amount)
    {
        if (!isInitializedInternal) return;
        CurrentHealth -= amount;
        // Debug.Log($"Player.TakeDamage: Took {amount} damage. CurrentHealth: {CurrentHealth}/{MaxHealth}", this);
        if (CurrentHealth < 0) CurrentHealth = 0;
        // if (CurrentHealth <= 0) Debug.Log($"{PlayerName} defeated (in TakeDamage)!", this);
    }

    public void Heal(int amount)
    {
        if (!isInitializedInternal) return;
        CurrentHealth += amount;
        if (CurrentHealth > MaxHealth) CurrentHealth = MaxHealth;
    }

    public void GainExperience(int amount)
    {
        if (!isInitializedInternal || amount <= 0) return;
        CurrentExperience += amount;
        Debug.Log($"{PlayerName} gained {amount} experience.", this);
        CheckForLevelUp();
    }

    private void CheckForLevelUp()
    {
        while (CurrentExperience >= ExperienceToNextLevel)
        {
            Level++;
            CurrentExperience -= ExperienceToNextLevel;
            ExperienceToNextLevel = Mathf.RoundToInt(ExperienceToNextLevel * 1.5f); // Example progression

            Attributes.Strength++; Attributes.Agility++; Attributes.Intelligence++; Attributes.Stamina++;
            Attributes.Wisdom++; Attributes.Fury++; Attributes.Endurance++; Attributes.Faith++;

            CalculateDerivedStats();
            RestoreToMaxStats();
            LogToCombatOrGameConsole($"{PlayerName} reached Level {Level}! Stats increased. HP/Resources refilled.");
        }
    }
    // Helper to decide where to log (could be moved to GameManager if preferred, or Player needs GM ref)
    private void LogToCombatOrGameConsole(string message)
    {
        if (GameManager.Instance != null)
        {
            // This is a simplified version. Ideally, Player shouldn't directly decide based on GameManager's state.
            // Events are better. For now, this will use the GameManager's logging.
            GameManager.Instance.SendMessageToPlayerLog(message); // We'll add this method to GameManager
        }
        else
        {
            Debug.Log(message, this); // Fallback
        }
    }


    public bool UseMana(int amount)
    {
        if (!isInitializedInternal || amount <= 0) return false;
        if (MaxMana == 0) { LogToCombatOrGameConsole("You do not use mana."); return false; }
        if (CurrentMana < amount) { LogToCombatOrGameConsole("Not enough mana!"); return false; }
        CurrentMana -= amount;
        return true;
    }
    public bool UseRage(int amount)
    {
        if (!isInitializedInternal || amount <= 0) return false;
        if (MaxRage == 0) { LogToCombatOrGameConsole("You do not use rage."); return false; }
        if (CurrentRage < amount) { LogToCombatOrGameConsole("Not enough rage!"); return false; }
        CurrentRage -= amount;
        return true;
    }
    public bool UseEnergy(int amount)
    {
        if (!isInitializedInternal || amount <= 0) return false;
        if (MaxEnergy == 0) { LogToCombatOrGameConsole("You do not use energy."); return false; }
        if (CurrentEnergy < amount) { LogToCombatOrGameConsole("Not enough energy!"); return false; }
        CurrentEnergy -= amount;
        return true;
    }


    public void AddGold(int amount)
    {
        if (amount > 0) Gold += amount;
    }

    public bool SpendGold(int amount)
    {
        if (amount > 0 && Gold >= amount) { Gold -= amount; return true; }
        return false;
    }

    public void AddItemToInventory(Item itemToAdd)
    {
        if (!isInitializedInternal || itemToAdd == null) return;
        if (itemToAdd.IsStackable)
        {
            Item existingItem = PlayerInventory.FirstOrDefault(i => i.Name == itemToAdd.Name && i.IsStackable);
            if (existingItem != null) existingItem.AddQuantity(itemToAdd.Quantity);
            else PlayerInventory.Add(new Item(itemToAdd.Name, itemToAdd.Description, itemToAdd.Type, itemToAdd.GoldValue, true, itemToAdd.Quantity));
        }
        else
        {
            PlayerInventory.Add(new Item(itemToAdd.Name, itemToAdd.Description, itemToAdd.Type, itemToAdd.GoldValue, false, 1));
        }
    }

    public bool RemoveItemFromInventory(string itemName, int quantityToRemove = 1)
    {
        if (!isInitializedInternal || quantityToRemove <= 0) return false;
        Item itemInInventory = PlayerInventory.FirstOrDefault(i => i.Name == itemName);
        if (itemInInventory == null) return false;

        if (itemInInventory.IsStackable)
        {
            if (itemInInventory.Quantity >= quantityToRemove)
            {
                itemInInventory.Quantity -= quantityToRemove;
                if (itemInInventory.Quantity <= 0) PlayerInventory.Remove(itemInInventory);
                return true;
            }
            return false;
        }
        else // Non-stackable
        {
            if (quantityToRemove == 1) return PlayerInventory.Remove(itemInInventory);
            // To remove multiple non-stackable, loop this or find multiple instances
            return false;
        }
    }

    public string GetPlayerStats()
    {
        if (!isInitializedInternal) return "Player data not available yet.";
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("--- Player Stats ---");
        sb.AppendLine($"Name: {PlayerName} ({Race} {Class} from {Origin})");
        sb.AppendLine($"Level: {Level} (XP: {CurrentExperience}/{ExperienceToNextLevel})");
        sb.AppendLine("--------------------");
        sb.AppendLine($"Health: {CurrentHealth} / {MaxHealth}");
        if (MaxMana > 0) sb.AppendLine($"Mana:   {CurrentMana} / {MaxMana} (WIS: {Attributes.Wisdom})");
        if (MaxRage > 0) sb.AppendLine($"Rage:   {CurrentRage} / {MaxRage} (FUR: {Attributes.Fury})");
        if (MaxEnergy > 0) sb.AppendLine($"Energy: {CurrentEnergy} / {MaxEnergy} (END: {Attributes.Endurance})");
        sb.AppendLine($"Gold: {Gold}g");
        sb.AppendLine("--------------------");
        sb.AppendLine("Attributes:");
        sb.AppendLine($"  Strength: {Attributes.Strength}  |  Agility: {Attributes.Agility}  |  Intelligence: {Attributes.Intelligence}");
        sb.AppendLine($"  Stamina: {Attributes.Stamina} |  Wisdom: {Attributes.Wisdom} |  Fury: {Attributes.Fury}");
        sb.AppendLine($"  Endurance: {Attributes.Endurance} |  Faith: {Attributes.Faith}");
        sb.AppendLine("--------------------");
        return sb.ToString();
    }

    public string GetInventoryList()
    {
        if (!isInitializedInternal) return "Player data not available yet.";
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("--- Inventory ---");
        if (PlayerInventory.Count == 0) sb.AppendLine("Your inventory is empty.");
        else foreach (Item item in PlayerInventory) sb.AppendLine($"- {item.ToString()}"); // Uses Item.ToString()
        sb.AppendLine($"\nGold: {Gold}g");
        sb.AppendLine("--------------------");
        return sb.ToString();
    }
}