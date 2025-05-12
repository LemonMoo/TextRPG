// File: Item.cs
using UnityEngine;

public enum ItemType // Example item types
{
    Generic, // For basic loot, vendor trash
    Weapon,
    Armor,
    Potion,
    QuestItem
}

[System.Serializable] // Makes it visible in the Inspector if you ever want to create items as assets
public class Item
{
    public string Name;
    public string Description;
    public ItemType Type;
    public int GoldValue;       // How much it's worth when selling (or its base gold drop if it's "Gold Pouch")
    public bool IsStackable;    // Can multiple instances of this item stack in one inventory slot?
    public int Quantity;        // Current quantity if stackable, or 1 if not

    // Constructor
    public Item(string name, string description, ItemType type, int goldValue, bool stackable = false, int quantity = 1)
    {
        Name = name;
        Description = description;
        Type = type;
        GoldValue = goldValue;
        IsStackable = stackable;
        Quantity = IsStackable ? quantity : 1; // Non-stackable items always have quantity 1 conceptually
    }

    // Method to increase quantity for stackable items
    public void AddQuantity(int amount)
    {
        if (IsStackable)
        {
            Quantity += amount;
        }
        else
        {
            Debug.LogWarning($"Tried to add quantity to non-stackable item: {Name}");
        }
    }

    // Override ToString for easy display (optional)
    public override string ToString()
    {
        return $"{Name}{(IsStackable && Quantity > 1 ? $" (x{Quantity})" : "")} - Value: {GoldValue}g";
    }
}