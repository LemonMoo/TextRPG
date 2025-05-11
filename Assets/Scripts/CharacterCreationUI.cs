using UnityEngine;
using UnityEngine.UI; // Required for Button
using TMPro;          // Required for TextMeshPro elements (TMP_Dropdown, TMP_InputField)
using System;         // Required for Enum
using System.Collections.Generic; // Required for List and Dictionary
using System.Linq;    // Required for LINQ methods like .ToList()

public class CharacterCreationUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Dropdown raceDropdown;
    public TMP_Dropdown classDropdown;
    public TMP_Dropdown originDropdown;
    public TMP_InputField nameInputField;
    public Button createCharacterButton;
    public TextMeshProUGUI feedbackText; // Optional: For displaying validation errors

    [Header("Default Settings")]
    public string defaultPlayerName = "Hero";

    // These will store the actual enum values of the player's choices
    private PlayerRace selectedRace;
    private PlayerClass selectedClass;
    private PlayerOrigin selectedOrigin;

    // Dictionary to hold race-specific origins, mapping a race to a list of its valid origins
    private Dictionary<PlayerRace, List<PlayerOrigin>> raceSpecificOrigins;
    // List to store the current valid origins for the selected race.
    // This is crucial for correctly mapping the dropdown's index to the actual PlayerOrigin enum value.
    private List<PlayerOrigin> currentValidOriginsForDropdown;

    void Start()
    {
        // Ensure all UI elements are assigned in the Inspector
        if (!ValidateReferences())
        {
            Debug.LogError("CharacterCreationUI: Critical UI references are missing. Disabling script.", this);
            this.enabled = false; // Disable the script if essential components aren't linked
            return;
        }

        // Initialize the mapping of races to their specific origins
        InitializeRaceSpecificOrigins();

        // Populate the Race and Class dropdowns with values from their respective enums
        PopulateDropdownWithEnumValues(raceDropdown, typeof(PlayerRace));
        PopulateDropdownWithEnumValues(classDropdown, typeof(PlayerClass));
        // The Origin dropdown will be populated dynamically when a race is selected (see OnRaceChanged)

        // Set the default player name in the input field
        nameInputField.text = defaultPlayerName;

        // --- Add Listeners to UI Elements ---
        // Call OnCreateCharacterClicked when the button is pressed
        createCharacterButton.onClick.AddListener(OnCreateCharacterClicked);

        // Update selected values and repopulate origins when dropdown selections change
        raceDropdown.onValueChanged.AddListener(delegate { OnRaceChanged(raceDropdown); });
        classDropdown.onValueChanged.AddListener(delegate { OnClassChanged(classDropdown); });
        originDropdown.onValueChanged.AddListener(delegate { OnOriginChanged(originDropdown); });

        // --- Initial Setup ---
        // Trigger OnRaceChanged to populate the origin dropdown based on the default selected race
        OnRaceChanged(raceDropdown);
        // Initialize selectedClass based on the default value of the class dropdown
        OnClassChanged(classDropdown);

        // Clear any initial feedback text
        if (feedbackText != null) feedbackText.text = "";
    }

    /// <summary>
    /// Validates that all required UI elements have been assigned in the Inspector.
    /// </summary>
    /// <returns>True if all references are valid, false otherwise.</returns>
    private bool ValidateReferences()
    {
        bool isValid = true;
        if (raceDropdown == null) { Debug.LogError("CharacterCreationUI: Race Dropdown not assigned!", this); isValid = false; }
        if (classDropdown == null) { Debug.LogError("CharacterCreationUI: Class Dropdown not assigned!", this); isValid = false; }
        if (originDropdown == null) { Debug.LogError("CharacterCreationUI: Origin Dropdown not assigned!", this); isValid = false; }
        if (nameInputField == null) { Debug.LogError("CharacterCreationUI: Name InputField not assigned!", this); isValid = false; }
        if (createCharacterButton == null) { Debug.LogError("CharacterCreationUI: Create Character Button not assigned!", this); isValid = false; }
        // feedbackText is optional, so not included in critical validation for disabling script
        return isValid;
    }

    /// <summary>
    /// Sets up the dictionary that maps each PlayerRace to its list of allowed PlayerOrigins.
    /// </summary>
    private void InitializeRaceSpecificOrigins()
    {
        raceSpecificOrigins = new Dictionary<PlayerRace, List<PlayerOrigin>>
        {
            // Define which origins are available for Humans
            {
                PlayerRace.Human, new List<PlayerOrigin>
                {
                    PlayerOrigin.CitizenOfStonecrest,
                    PlayerOrigin.AcolyteOfTheSunTemple,
                    PlayerOrigin.ReaverOfTheBrokenCoast
                }
            },
            // Define which origins are available for Elves
            {
                PlayerRace.Elf, new List<PlayerOrigin>
                {
                    PlayerOrigin.WhisperwindForestDweller,
                    PlayerOrigin.LoremasterOfSilverspire,
                    PlayerOrigin.ShadowWalkerOfTheHiddenPaths
                }
            },
            // Define which origins are available for Dwarves
            {
                PlayerRace.Dwarf, new List<PlayerOrigin>
                {
                    PlayerOrigin.ClanHoldOfIronpeak,
                    PlayerOrigin.DeepRoadsProspector,
                    PlayerOrigin.GuardianOfTheAncestralTombs
                }
            },
            // Define which origins are available for Orcs
            {
                PlayerRace.Orc, new List<PlayerOrigin>
                {
                    PlayerOrigin.BloodfangTribeWarrior,
                    PlayerOrigin.SpiritCallerOfTheAshPlains,
                    PlayerOrigin.StrongholdArtificer
                }
            },
            // Define which origins are available for Trolls
            {
                PlayerRace.Troll, new List<PlayerOrigin>
                {
                    PlayerOrigin.MossrockValleyGuardian,
                    PlayerOrigin.RiverbendShaman,
                    PlayerOrigin.HighPeakClanMember
                }
            }
            // Ensure all PlayerRace enum values have an entry here if they should have specific origins.
        };
        // This list will hold the actual PlayerOrigin enum values for the currently selected race's origins
        currentValidOriginsForDropdown = new List<PlayerOrigin>();
    }

    /// <summary>
    /// Populates a TMP_Dropdown with the names of the values from a given Enum type.
    /// </summary>
    /// <param name="dropdown">The TMP_Dropdown UI element to populate.</param>
    /// <param name="enumType">The Type of the enum to get values from (e.g., typeof(PlayerRace)).</param>
    private void PopulateDropdownWithEnumValues(TMP_Dropdown dropdown, Type enumType)
    {
        if (dropdown == null) return;

        // Get all the names from the enum
        List<string> names = new List<string>(Enum.GetNames(enumType));
        // Format the names for better display (e.g., "CitizenOfStonecrest" to "Citizen Of Stonecrest")
        List<string> formattedNames = names.Select(name => FormatEnumString(name)).ToList();

        dropdown.ClearOptions(); // Remove any existing options
        dropdown.AddOptions(formattedNames); // Add the new, formatted options
        dropdown.RefreshShownValue(); // Update the dropdown to show the first item
    }

    /// <summary>
    /// Called when the selected value in the Race dropdown changes.
    /// Updates the selectedRace and repopulates the Origin dropdown based on the new race.
    /// </summary>
    private void OnRaceChanged(TMP_Dropdown change)
    {
        // The dropdown's value is the index of the selected option.
        // Cast this index to the PlayerRace enum.
        selectedRace = (PlayerRace)change.value;
        // Debug.Log("Selected Race: " + selectedRace + " (Index: " + change.value + ")");

        // Repopulate the origin dropdown with origins valid for the newly selected race
        PopulateOriginDropdownForRace(selectedRace);
    }

    /// <summary>
    /// Populates the Origin dropdown with origins specific to the given PlayerRace.
    /// </summary>
    /// <param name="race">The PlayerRace for which to show origins.</param>
    private void PopulateOriginDropdownForRace(PlayerRace race)
    {
        originDropdown.ClearOptions(); // Clear previous origin options
        currentValidOriginsForDropdown.Clear(); // Clear the list that maps dropdown index to PlayerOrigin enum value

        // Try to get the list of origins for the specified race from our dictionary
        if (raceSpecificOrigins.TryGetValue(race, out List<PlayerOrigin> originsForRace))
        {
            List<string> originDisplayNames = new List<string>();
            foreach (PlayerOrigin origin in originsForRace)
            {
                originDisplayNames.Add(FormatEnumString(origin.ToString())); // Format the name for display
                currentValidOriginsForDropdown.Add(origin); // Add the actual enum value to our tracking list
            }
            originDropdown.AddOptions(originDisplayNames);
            originDropdown.interactable = true; // Make sure the dropdown is usable

            // If there are valid origins, select the first one by default
            if (currentValidOriginsForDropdown.Count > 0)
            {
                originDropdown.value = 0; // Set dropdown to show the first item
                OnOriginChanged(originDropdown); // Manually trigger update for selectedOrigin
            }
            else // This case should ideally not be hit if all races have defined origins
            {
                originDropdown.interactable = false;
                originDropdown.AddOptions(new List<string> { "No Origins Available" });
                Debug.LogWarning($"No origins are defined in raceSpecificOrigins for race: {race}", this);
            }
        }
        else // This race isn't in our raceSpecificOrigins dictionary
        {
            originDropdown.interactable = false;
            originDropdown.AddOptions(new List<string> { "Origins Not Defined" });
            Debug.LogWarning($"The race '{race}' was not found as a key in the raceSpecificOrigins dictionary.", this);
        }
        originDropdown.RefreshShownValue(); // Update the dropdown display
    }

    /// <summary>
    /// Called when the selected value in the Class dropdown changes.
    /// Updates the selectedClass.
    /// </summary>
    private void OnClassChanged(TMP_Dropdown change)
    {
        selectedClass = (PlayerClass)change.value;
        // Debug.Log("Selected Class: " + selectedClass  + " (Index: " + change.value + ")");
    }

    /// <summary>
    /// Called when the selected value in the Origin dropdown changes.
    /// Updates the selectedOrigin based on the current list of valid origins for the selected race.
    /// </summary>
    private void OnOriginChanged(TMP_Dropdown change)
    {
        // Ensure the selected index is valid for the current list of origins
        if (currentValidOriginsForDropdown.Count > 0 && change.value >= 0 && change.value < currentValidOriginsForDropdown.Count)
        {
            selectedOrigin = currentValidOriginsForDropdown[change.value];
            // Debug.Log("Selected Origin: " + selectedOrigin + " (Index: " + change.value + ")");
        }
        else if (currentValidOriginsForDropdown.Count == 0)
        {
            // This handles the case where a race might have no origins listed.
            // selectedOrigin will retain its previous value or needs a defined default if this state is problematic.
            // For now, we just log if the selection is attempted on an empty or invalid list.
            // Debug.LogWarning("Origin selection changed, but no valid origins currently available or index is out of bounds.", this);
        }
    }

    /// <summary>
    /// Formats an enum name string for better display by inserting spaces before capital letters.
    /// Example: "CitizenOfStonecrest" becomes "Citizen Of Stonecrest".
    /// </summary>
    /// <param name="enumName">The raw string name of the enum value.</param>
    /// <returns>A formatted string with spaces.</returns>
    private string FormatEnumString(string enumName)
    {
        if (string.IsNullOrEmpty(enumName)) return "";
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append(enumName[0]); // Start with the first character
        for (int i = 1; i < enumName.Length; i++)
        {
            // If the current character is uppercase, and the previous isn't already a space (to avoid double spaces)
            if (char.IsUpper(enumName[i]) && enumName[i - 1] != ' ')
            {
                sb.Append(' '); // Insert a space before the capital letter
            }
            sb.Append(enumName[i]);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Called when the "Create Character" button is clicked.
    /// Validates input, gathers selected data, and tells the GameManager to finalize character creation.
    /// </summary>
    private void OnCreateCharacterClicked()
    {
        string playerName = nameInputField.text;

        // --- Basic Input Validation ---
        if (string.IsNullOrWhiteSpace(playerName))
        {
            DisplayFeedback("Player name cannot be empty.");
            return;
        }
        if (playerName.Length > 20) // Arbitrary length limit for player name
        {
            DisplayFeedback("Player name is too long (max 20 characters).");
            return;
        }

        // Ensure an origin is actually selected, especially if a race had no origins (though our setup aims to prevent this)
        if (currentValidOriginsForDropdown.Count == 0 || originDropdown.value < 0 || originDropdown.value >= currentValidOriginsForDropdown.Count)
        {
            DisplayFeedback($"Please select a valid origin for {selectedRace}.");
            // This might occur if PopulateOriginDropdownForRace failed to find origins or if the dropdown is somehow in an invalid state.
            return;
        }
        // selectedOrigin should be correctly set by OnOriginChanged by this point.

        DisplayFeedback(""); // Clear any previous feedback messages

        // --- Create Base Attributes ---
        // All characters start with default base attributes.
        // The Player.cs script's ApplyAttributeModifiers() method will then adjust these based on class/race.
        Attributes startingAttributes = new Attributes(); // Uses default constructor values (e.g., all 5s)

        Debug.Log($"CharacterCreationUI: Attempting to create character. Name='{playerName}', Race='{selectedRace}', Class='{selectedClass}', Origin='{selectedOrigin}'", this);

        // --- Call GameManager to Finalize Creation ---
        if (GameManager.Instance != null)
        {
            GameManager.Instance.FinishCharacterCreation(playerName, selectedRace, selectedClass, selectedOrigin, startingAttributes);
        }
        else
        {
            Debug.LogError("CharacterCreationUI: GameManager.Instance is null. Cannot finalize character creation.", this);
            DisplayFeedback("Critical Error: Could not create character. GameManager is missing.");
        }
    }

    /// <summary>
    /// Displays a message in the feedbackText UI element.
    /// </summary>
    /// <param name="message">The message to display.</param>
    private void DisplayFeedback(string message)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
        }
        else if (!string.IsNullOrEmpty(message)) // Log to console if feedbackText UI element isn't assigned
        {
            Debug.LogWarning("CharacterCreationUI Feedback: " + message + " (FeedbackText UI element not assigned)", this);
        }
    }
}
