// File: CommandParser.cs
using UnityEngine;
using System.Linq; // Required for ToLower, Split, etc.
using System.Collections.Generic; // Required for lists if we add more complex command handling

public class CommandParser : MonoBehaviour
{
    // Assign these in the Inspector or ensure they are found by FindFirstObjectByType
    public Player player;
    public LocationManager locationManager;

    // A simple list of recognized verbs for the help command
    private List<string> recognizedCommands = new List<string>()
    {
        "go [direction]",
        "north (or n)",
        "south (or s)",
        "east (or e)",
        "west (or w)",
        "look (or l)",
        "inventory (or inv, i)", // Added 'i' as a shortcut
        "stats (or stat, char)", // Added shortcuts
        "help (or ?)",           // Added '?' as a shortcut
        "quit (or exit)"
        // Add more commands as they are implemented (e.g., "get [item]", "use [item]")
    };


    void Start()
    {
        // Attempt to find Player and LocationManager if not assigned in Inspector
        if (player == null)
        {
            player = FindFirstObjectByType<Player>();
            if (player == null)
            {
                Debug.LogError("CommandParser: Player script not found in the scene!", this);
            }
        }
        if (locationManager == null)
        {
            locationManager = FindFirstObjectByType<LocationManager>();
            if (locationManager == null)
            {
                Debug.LogError("CommandParser: LocationManager script not found in the scene!", this);
            }
        }
    }

    /// <summary>
    /// Parses the raw input string from the player and attempts to execute a command.
    /// </summary>
    /// <param name="rawInput">The command string entered by the player.</param>
    /// <returns>A string representing the result or feedback of the command.</returns>
    public string ParseCommand(string rawInput)
    {
        if (string.IsNullOrWhiteSpace(rawInput))
        {
            return "Please enter a command.";
        }

        string input = rawInput.Trim().ToLower();
        string[] commandParts = input.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);

        if (commandParts.Length == 0)
        {
            return "Please enter a command.";
        }

        string verb = commandParts[0];
        // string argument = commandParts.Length > 1 ? commandParts[1] : null;
        // For commands with more than one word in argument (e.g. "get shiny rock")
        string argument = commandParts.Length > 1 ? string.Join(" ", commandParts.Skip(1)) : null;


        // --- Command Processing ---
        switch (verb)
        {
            case "go":
                if (argument != null)
                {
                    if (locationManager != null && locationManager.TryMovePlayer(argument))
                    {
                        return GetLocationLookDescription(); // Returns new location's full description
                    }
                    else
                    {
                        return $"You can't go {argument}.";
                    }
                }
                else
                {
                    return "Go where? (e.g., 'go north')";
                }

            // Single-word directional commands
            case "north":
            case "n":
                if (locationManager != null && locationManager.TryMovePlayer("north")) { return GetLocationLookDescription(); }
                else { return "You can't go north."; }
            case "south":
            case "s":
                if (locationManager != null && locationManager.TryMovePlayer("south")) { return GetLocationLookDescription(); }
                else { return "You can't go south."; }
            case "east":
            case "e":
                if (locationManager != null && locationManager.TryMovePlayer("east")) { return GetLocationLookDescription(); }
                else { return "You can't go east."; }
            case "west":
            case "w":
                if (locationManager != null && locationManager.TryMovePlayer("west")) { return GetLocationLookDescription(); }
                else { return "You can't go west."; }

            case "look":
            case "l":
                return GetLocationLookDescription();

            case "inventory":
            case "inv":
            case "i": // Added 'i'
                if (player != null)
                {
                    return player.GetInventoryList(); // Uses the updated method from Player.cs
                }
                return "Cannot access inventory. Player script not found.";

            case "stats":
            case "stat":
            case "character":
            case "char": // Added shortcuts
                if (player != null)
                {
                    return player.GetPlayerStats(); // Uses the updated method from Player.cs (should include gold)
                }
                return "Cannot display stats. Player script not found.";

            case "help":
            case "?": // Added '?'
                System.Text.StringBuilder helpBuilder = new System.Text.StringBuilder("Available commands:\n");
                foreach (string cmd in recognizedCommands)
                {
                    helpBuilder.AppendLine("- " + cmd);
                }
                helpBuilder.AppendLine("\nType 'quit' or 'exit' to leave the game.");
                return helpBuilder.ToString();

            case "quit":
            case "exit":
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
                return "Quitting game...";

            // --- Placeholder for future commands ---
            // case "get":
            // case "take":
            //     if (argument != null) return $"You try to get '{argument}'. (Not implemented)";
            //     else return "Get what?";
            // case "use":
            //     if (argument != null) return $"You try to use '{argument}'. (Not implemented)";
            //     else return "Use what?";
            // case "talk":
            //     if (argument != null) return $"You try to talk to '{argument}'. (Not implemented)";
            //     else return "Talk to whom?";

            default:
                return $"Unknown command: '{verb}'. Type 'help' for a list of commands.";
        }
    }

    /// <summary>
    /// Helper function to get the full description of the current location.
    /// </summary>
    private string GetLocationLookDescription()
    {
        if (locationManager != null && locationManager.CurrentLocation != null)
        {
            System.Text.StringBuilder lookResult = new System.Text.StringBuilder();
            lookResult.AppendLine($"Location: {locationManager.CurrentLocation.Name}");
            lookResult.AppendLine(locationManager.CurrentLocation.Description);
            lookResult.AppendLine(locationManager.CurrentLocation.GetExitsDescription());
            // Future: Add items in location, NPCs, etc.
            // lookResult.AppendLine(locationManager.CurrentLocation.GetItemsDescription());
            // lookResult.AppendLine(locationManager.CurrentLocation.GetNPCsDescription());
            return lookResult.ToString();
        }
        return "You don't seem to be anywhere particular, or the location manager isn't working.";
    }

    // Example of how this might be called by GameConsoleUI (already in your GameConsoleUI.cs)
    // public void ProcessPlayerInputFromUI(string inputText)
    // {
    //     if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing)
    //     {
    //         string commandResult = ParseCommand(inputText);
    //         // Send commandResult to GameConsoleUI.AddMessageToOutput(commandResult)
    //     }
    // }
}