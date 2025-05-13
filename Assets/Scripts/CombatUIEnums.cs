// File: CombatUIEnums.cs

public enum CombatMenuState
{
    Main,      // Showing ATTACK/SKILL/ITEM/FLEE
    Skills,    // Showing list of skills
    Items      // Future: Showing list of usable items
    // Spells    // Future: For casters if their spell list is long
}

// FighterSkillType enum
public enum FighterSkillType
{
    None, // Placeholder if no skill selected from sub-menu or for error states
    Counterattack
    // PowerStrike, // Future skill
    // DefensiveStance // Future skill
}

// You could add other UI-related enums here in the future,
// for example, if you had different types of item sub-menus.