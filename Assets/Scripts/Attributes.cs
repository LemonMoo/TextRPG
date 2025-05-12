// File: Attributes.cs
using UnityEngine; // Often included, though not strictly necessary for this simple class

[System.Serializable] // This allows Unity to show it in the Inspector if it's a public field in a MonoBehaviour
public class Attributes
{
    public int Strength;
    public int Agility;
    public int Intelligence;
    public int Stamina;
    public int Wisdom;
    public int Fury;
    public int Endurance;
    public int Faith;

    // Base constructor with default values
    public Attributes(int str = 5, int agi = 5, int intel = 5, int sta = 5, int wis = 3, int fur = 3, int end = 3, int fai = 3)
    {
        Strength = str;
        Agility = agi;
        Intelligence = intel;
        Stamina = sta;
        Wisdom = wis;
        Fury = fur;
        Endurance = end;
        Faith = fai;
    }

    // You could add a copy constructor if needed, e.g., when creating enemy stats from a template
    public Attributes(Attributes source)
    {
        Strength = source.Strength;
        Agility = source.Agility;
        Intelligence = source.Intelligence;
        Stamina = source.Stamina;
        Wisdom = source.Wisdom;
        Fury = source.Fury;
        Endurance = source.Endurance;
        Faith = source.Faith;
    }
}