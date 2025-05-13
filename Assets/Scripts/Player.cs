// File: Player.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// PlayerRace, PlayerClass, PlayerOrigin, Attributes, Item enums/classes should be defined elsewhere

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

    private bool isInitializedInternal = false;
    public bool IsInitialized => isInitializedInternal;

    public bool IsCounterattackActive { get; private set; } = false;
    private const int COUNTERATTACK_RAGE_COST = 10;

    // Private helper method to route player-specific messages through GameManager
    private void LogPlayerMessage(string message)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SendMessageToPlayerLog(message);
        }
        else
        {
            Debug.Log($"Player Log (GM Missing): {message}", this); // Fallback if GM not available
        }
    }

    public void InitializePlayer(string playerName, PlayerRace race, PlayerClass playerClass, PlayerOrigin origin, Attributes baseAttributes)
    {
        if (isInitializedInternal) { Debug.LogWarning("Player already initialized.", this); return; }

        PlayerName = playerName; Race = race; Class = playerClass; Origin = origin; Attributes = baseAttributes;

        ApplyAttributeModifiers();
        CalculateDerivedStats();

        CurrentHealth = MaxHealth;
        CurrentMana = MaxMana;
        CurrentRage = 0; // Fighters start with 0 Rage
        CurrentEnergy = MaxEnergy;

        PlayerInventory = new List<Item>(); Gold = 25; // Example starting gold
        Level = 1; CurrentExperience = 0; ExperienceToNextLevel = 100;

        isInitializedInternal = true;
        // Initializing message could be logged by GameManager after this call, or here:
        // LogPlayerMessage($"Player '{PlayerName}' the {Race} {Class} is ready!");
        Debug.Log($"Player '{PlayerName}' initialized. Class: {Class}, HP: {CurrentHealth}/{MaxHealth}, Rage: {CurrentRage}/{MaxRage}, Gold: {Gold}. IsInitialized: {IsInitialized}", this);
    }

    private void ApplyAttributeModifiers()
    {
        if (Attributes == null) Attributes = new Attributes();
        int baseResourceStat = 3; Attributes.Wisdom = baseResourceStat; Attributes.Fury = baseResourceStat; Attributes.Endurance = baseResourceStat; Attributes.Faith = baseResourceStat;
        int primaryStatBoost = 7, secondaryStatBoost = 4;
        switch (Class)
        {
            case PlayerClass.Fighter: Attributes.Strength += primaryStatBoost; Attributes.Fury += primaryStatBoost; Attributes.Stamina += secondaryStatBoost; break;
            case PlayerClass.Wizard: Attributes.Intelligence += primaryStatBoost; Attributes.Wisdom += primaryStatBoost; break;
            case PlayerClass.Scout: Attributes.Agility += primaryStatBoost; Attributes.Endurance += primaryStatBoost; break;
            case PlayerClass.Ranger: Attributes.Agility += primaryStatBoost; Attributes.Wisdom += secondaryStatBoost; break;
            case PlayerClass.Cleric: Attributes.Faith += primaryStatBoost; Attributes.Wisdom += primaryStatBoost; Attributes.Stamina += secondaryStatBoost; break;
        }
    }

    public void CalculateDerivedStats()
    {
        if (Attributes == null) { Attributes = new Attributes(); Debug.LogError("Player Attributes null in CalculateDerivedStats!", this); }
        MaxHealth = Attributes.Stamina * HEALTH_PER_STAMINA_POINT;
        MaxMana = Attributes.Wisdom * MANA_PER_WISDOM_POINT;
        MaxRage = Attributes.Fury * RAGE_PER_FURY_POINT;
        MaxEnergy = Attributes.Endurance * ENERGY_PER_ENDURANCE_POINT;
    }

    public void RestoreToMaxStats()
    {
        CurrentHealth = MaxHealth;
        CurrentMana = MaxMana;
        CurrentRage = 0; // Rage resets, does not fill
        CurrentEnergy = MaxEnergy;
        LogPlayerMessage("Your health and resources have been restored (Rage reset to 0).");
    }

    public void TakeDamage(int amount)
    {
        if (!isInitializedInternal || amount <= 0) return;
        // Counterattack check is handled by GameManager before this would be called with full damage

        CurrentHealth -= amount;
        if (CurrentHealth < 0) CurrentHealth = 0;

        // GameManager's EnemyTurnSequence logs the actual damage taken and current HP.
        // Here, we only log the additional Rage gain if applicable.
        if (Class == PlayerClass.Fighter && MaxRage > 0)
        {
            int rageGained = Mathf.Clamp(amount / 3, 1, 10); // Example: 1/3rd damage as Rage
            if (rageGained > 0) // Only generate if there's rage to gain
            {
                GenerateRage(rageGained); // GenerateRage itself should be silent mostly
                LogPlayerMessage($"You gain {rageGained} Rage from the hit! ({CurrentRage}/{MaxRage})");
            }
        }
    }

    public void GenerateRage(int amount)
    {
        if (!isInitializedInternal || amount <= 0 || Class != PlayerClass.Fighter || MaxRage == 0) return;
        int oldRage = CurrentRage;
        CurrentRage = Mathf.Min(CurrentRage + amount, MaxRage);
        // Optionally log here if rage changes, or let the action causing rage gain log it.
        // if (CurrentRage != oldRage) LogPlayerMessage($"Rage: {CurrentRage}/{MaxRage}");
    }

    public bool SpendRage(int amount)
    {
        if (!isInitializedInternal || amount <= 0 || Class != PlayerClass.Fighter) return false;
        if (CurrentRage >= amount)
        {
            CurrentRage -= amount;
            // LogPlayerMessage($"Spent {amount} Rage. Current: {CurrentRage}/{MaxRage}"); // Action using rage should confirm
            return true;
        }
        LogPlayerMessage("Not enough Rage!");
        return false;
    }

    public bool CanActivateCounterattack()
    {
        return Class == PlayerClass.Fighter && CurrentRage >= COUNTERATTACK_RAGE_COST && !IsCounterattackActive;
    }

    public bool ActivateCounterattack()
    {
        if (IsCounterattackActive) { LogPlayerMessage("Counterattack is already active!"); return false; }
        if (Class != PlayerClass.Fighter) { LogPlayerMessage("Only Fighters can prepare a Counterattack."); return false; }

        if (CurrentRage >= COUNTERATTACK_RAGE_COST)
        {
            if (SpendRage(COUNTERATTACK_RAGE_COST)) // SpendRage logs "Not enough rage" if it somehow fails here
            {
                IsCounterattackActive = true;
                LogPlayerMessage("You ready a Counterattack!");
                return true;
            }
            return false; // Should not be reached if CurrentRage check passed and SpendRage worked
        }
        else
        {
            LogPlayerMessage($"Not enough Rage for Counterattack! (Need {COUNTERATTACK_RAGE_COST}, Have {CurrentRage})");
            return false;
        }
    }

    public void ConsumeCounterattackBuff()
    {
        if (IsCounterattackActive)
        {
            IsCounterattackActive = false;
            LogPlayerMessage("Your Counterattack stance fades.");
        }
    }

    public int GetBasicAttackDamage()
    {
        return Mathf.Max(1, Random.Range(Attributes.Strength, Attributes.Strength + 5));
    }

    public void Heal(int amount)
    {
        if (!isInitializedInternal || amount <= 0) return;
        CurrentHealth += amount;
        if (CurrentHealth > MaxHealth) CurrentHealth = MaxHealth;
        LogPlayerMessage($"You heal for {amount} health. Current HP: {CurrentHealth}/{MaxHealth}");
    }

    public void GainExperience(int amount)
    {
        if (!isInitializedInternal || amount <= 0) return;
        CurrentExperience += amount;
        LogPlayerMessage($"{PlayerName} gained {amount} experience.");
        CheckForLevelUp();
    }

    private void CheckForLevelUp()
    {
        while (isInitializedInternal && CurrentExperience >= ExperienceToNextLevel && ExperienceToNextLevel > 0)
        {
            Level++;
            int prevMaxHealth = MaxHealth; // Store previous max health to show increase
            CurrentExperience -= ExperienceToNextLevel;
            ExperienceToNextLevel = Mathf.RoundToInt(ExperienceToNextLevel * 1.5f);
            if (ExperienceToNextLevel <= 0) ExperienceToNextLevel = int.MaxValue; // Safety for max level

            // Stat increases
            Attributes.Strength++; Attributes.Agility++; Attributes.Intelligence++; Attributes.Stamina++;
            Attributes.Wisdom++; Attributes.Fury++; Attributes.Endurance++; Attributes.Faith++;

            CalculateDerivedStats(); // Recalculate MaxHealth, MaxMana etc.
            int healthGain = MaxHealth - prevMaxHealth;
            CurrentHealth += healthGain; // Add the gain from new max health to current health too
            if (CurrentHealth > MaxHealth) CurrentHealth = MaxHealth; // Cap it
            // Restore other resources to max (or a portion) upon level up
            CurrentMana = MaxMana;
            CurrentEnergy = MaxEnergy;
            // CurrentRage is not typically refilled on level up, but rather kept or slightly increased. For now, no change.

            LogPlayerMessage($"{PlayerName} reached Level {Level}! All attributes increased by 1. Max Health increased. Resources refilled (Rage unchanged). Next level at {ExperienceToNextLevel} XP.");
        }
    }

    public bool UseMana(int amount)
    {
        if (!isInitializedInternal || amount <= 0) return false;
        if (MaxMana == 0) { LogPlayerMessage("You do not use mana."); return false; }
        if (CurrentMana < amount) { LogPlayerMessage("Not enough mana!"); return false; }
        CurrentMana -= amount;
        // LogPlayerMessage($"Used {amount} mana. Current: {CurrentMana}/{MaxMana}"); // Action using mana should confirm
        return true;
    }

    // Note: No direct "UseRage" method as skills will call SpendRage.
    // If you had a generic rage dump, you'd add it here.

    public bool UseEnergy(int amount)
    {
        if (!isInitializedInternal || amount <= 0) return false;
        if (MaxEnergy == 0) { LogPlayerMessage("You do not use energy."); return false; }
        if (CurrentEnergy < amount) { LogPlayerMessage("Not enough energy!"); return false; }
        CurrentEnergy -= amount;
        // LogPlayerMessage($"Used {amount} energy. Current: {CurrentEnergy}/{MaxEnergy}"); // Action using energy should confirm
        return true;
    }

    public void AddGold(int amount) { if (isInitializedInternal && amount > 0) Gold += amount; }
    public bool SpendGold(int amount) { if (isInitializedInternal && amount > 0 && Gold >= amount) { Gold -= amount; return true; } return false; }

    public void AddItemToInventory(Item itemToAdd) { if (!isInitializedInternal || itemToAdd == null) return; if (itemToAdd.IsStackable) { Item eI = PlayerInventory.FirstOrDefault(i => i.Name == itemToAdd.Name && i.IsStackable); if (eI != null) eI.AddQuantity(itemToAdd.Quantity); else PlayerInventory.Add(new Item(itemToAdd.Name, itemToAdd.Description, itemToAdd.Type, itemToAdd.GoldValue, true, itemToAdd.Quantity)); } else PlayerInventory.Add(new Item(itemToAdd.Name, itemToAdd.Description, itemToAdd.Type, itemToAdd.GoldValue, false, 1)); }
    public bool RemoveItemFromInventory(string itemName, int quantityToRemove = 1) { if (!isInitializedInternal || quantityToRemove <= 0) return false; Item iII = PlayerInventory.FirstOrDefault(i => i.Name == itemName); if (iII == null) return false; if (iII.IsStackable) { if (iII.Quantity >= quantityToRemove) { iII.Quantity -= quantityToRemove; if (iII.Quantity <= 0) PlayerInventory.Remove(iII); return true; } return false; } else { if (quantityToRemove == 1) return PlayerInventory.Remove(iII); return false; } }

    public string GetPlayerStats()
    {
        if (!isInitializedInternal) return "Player data not available.";
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("--- Player Stats ---");
        sb.AppendLine($"Name: {PlayerName} ({Race} {Class} from {Origin})");
        sb.AppendLine($"Level: {Level} (XP: {CurrentExperience}/{ExperienceToNextLevel})");
        sb.AppendLine($"Gold: {Gold}g");
        sb.AppendLine("--------------------");
        sb.AppendLine($"Health: {CurrentHealth} / {MaxHealth}");
        if (MaxMana > 0 || Class == PlayerClass.Wizard || Class == PlayerClass.Ranger || Class == PlayerClass.Cleric) sb.AppendLine($"Mana:   {CurrentMana} / {MaxMana} (WIS: {Attributes.Wisdom})");
        if (MaxRage > 0 || Class == PlayerClass.Fighter) sb.AppendLine($"Rage:   {CurrentRage} / {MaxRage} (FUR: {Attributes.Fury})");
        if (MaxEnergy > 0 || Class == PlayerClass.Scout) sb.AppendLine($"Energy: {CurrentEnergy} / {MaxEnergy} (END: {Attributes.Endurance})");
        sb.AppendLine("--------------------");
        sb.AppendLine("Attributes:");
        sb.AppendLine($"  STR: {Attributes.Strength} | AGI: {Attributes.Agility} | INT: {Attributes.Intelligence}");
        sb.AppendLine($"  STA: {Attributes.Stamina} | WIS: {Attributes.Wisdom} | FUR: {Attributes.Fury}");
        sb.AppendLine($"  END: {Attributes.Endurance} | FAI: {Attributes.Faith}");
        sb.AppendLine("--------------------");
        return sb.ToString();
    }
    public string GetInventoryList()
    {
        if (!isInitializedInternal) return "Player data not available.";
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("--- Inventory ---");
        if (PlayerInventory.Count == 0) sb.AppendLine("Your inventory is empty.");
        else foreach (Item item in PlayerInventory) sb.AppendLine($"- {item.ToString()}"); // Uses Item.ToString()
        sb.AppendLine($"\nGold: {Gold}g");
        sb.AppendLine("--------------------");
        return sb.ToString();
    }
}