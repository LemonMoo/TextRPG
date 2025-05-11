using UnityEngine;
using System.Linq; // Required for ToLower, Split, etc.
using System.Collections.Generic; // Required for lists if we add more complex command handling

public class CommandParser : MonoBehaviour
{
    public Player player; // Assign your Player GameObject/script here in the Inspector
    public LocationManager locationManager; // Assign your LocationManager GameObject/script here

    // A simple list of recognized verbs for the help command
    private List<string> recognizedCommands = new List<string>()
    {
        "go [direction]",
        "north (or n)",
        "south (or s)",
        "east (or e)",
        "west (or w)",
        "look (or l)",
        "inventory (or inv)",
        "stats",
        "help",
        "quit"
        // Add more commands as they are implemented
    };


    void Start()
    {
        // Attempt to find Player and LocationManager if not assigned in Inspector
        if (player == null)
        {
            // Updated to use FindFirstObjectByType as recommended
            player = FindFirstObjectByType<Player>();
            if (player == null)
            {
                Debug.LogError("CommandParser: Player script not found in the scene!");
            }
        }
        if (locationManager == null)
        {
            // Updated to use FindFirstObjectByType as recommended
            locationManager = FindFirstObjectByType<LocationManager>();
            if (locationManager == null)
            {
                Debug.LogError("CommandParser: LocationManager script not found in the scene!");
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

        string input = rawInput.Trim().ToLower(); // Normalize input: remove whitespace, convert to lowercase
        string[] commandParts = input.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);

        if (commandParts.Length == 0)
        {
            return "Please enter a command.";
        }

        string verb = commandParts[0];
        string argument = commandParts.Length > 1 ? commandParts[1] : null;
        // For commands with more than one argument, you might need:
        // string[] arguments = commandParts.Skip(1).ToArray();

        // --- Command Processing ---
        switch (verb)
        {
            case "go":
                if (argument != null)
                {
                    if (locationManager.TryMovePlayer(argument))
                    {
                        // LocationManager.DisplayCurrentLocationInfo() is called internally on successful move.
                        // We can return the new location's brief info or just a success message.
                        // For now, DisplayCurrentLocationInfo logs to console.
                        // We'll adjust this when we build the UI.
                        // Return the description of the new location directly
                        return GetLocationLookDescription();
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
                if (locationManager.TryMovePlayer("north")) { return GetLocationLookDescription(); }
                else { return "You can't go north."; }
            case "south":
            case "s":
                if (locationManager.TryMovePlayer("south")) { return GetLocationLookDescription(); }
                else { return "You can't go south."; }
            case "east":
            case "e":
                if (locationManager.TryMovePlayer("east")) { return GetLocationLookDescription(); }
                else { return "You can't go east."; }
            case "west":
            case "w":
                if (locationManager.TryMovePlayer("west")) { return GetLocationLookDescription(); }
                else { return "You can't go west."; }

            case "look":
            case "l":
                return GetLocationLookDescription();

            case "inventory":
            case "inv":
                if (player != null)
                {
                    if (player.Inventory.Count == 0)
                    {
                        return "Your inventory is empty.";
                    }
                    System.Text.StringBuilder invResult = new System.Text.StringBuilder("Inventory:\n");
                    foreach (string item in player.Inventory)
                    {
                        invResult.AppendLine("- " + item);
                    }
                    return invResult.ToString();
                }
                return "Cannot access inventory.";

            case "stats":
            case "character":
                if (player != null)
                {
                    return player.GetPlayerStats(); // This method already returns a formatted string.
                }
                return "Cannot display stats.";

            case "help":
                System.Text.StringBuilder helpBuilder = new System.Text.StringBuilder("Available commands:\n");
                foreach (string cmd in recognizedCommands)
                {
                    helpBuilder.AppendLine("- " + cmd);
                }
                helpBuilder.AppendLine("\nType 'quit' to exit the game.");
                return helpBuilder.ToString();

            case "quit":
            case "exit":
                // In a standalone build, this would close the application.
                // In the Unity Editor, this will stop play mode.
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
                return "Quitting game..."; // This message might not be seen if the game closes too fast.

            default:
                return $"Unknown command: '{verb}'. Type 'help' for a list of commands.";
        }
    }

    /// <summary>
    /// Helper function to get the full description of the current location.
    /// This is used by 'look' and after successful movement.
    /// </summary>
    /// <returns>A string describing the current location.</returns>
    private string GetLocationLookDescription()
    {
        if (locationManager != null && locationManager.CurrentLocation != null)
        {
            System.Text.StringBuilder lookResult = new System.Text.StringBuilder();
            lookResult.AppendLine($"Location: {locationManager.CurrentLocation.Name}");
            lookResult.AppendLine(locationManager.CurrentLocation.Description);
            lookResult.AppendLine(locationManager.CurrentLocation.GetExitsDescription());
            // Future: Add items in location, NPCs, etc.
            // if (locationManager.CurrentLocation.ItemsInLocation.Count > 0) { ... }
            // if (locationManager.CurrentLocation.NPCsInLocation.Count > 0) { ... }
            return lookResult.ToString();
        }
        return "You don't seem to be anywhere particular, or the location manager isn't working.";
    }


    // Example of how you might call this from another script (e.g., your UI input field)
    public void ProcessPlayerInput(string inputText)
    {
        string commandResult = ParseCommand(inputText);
        Debug.Log("Output: \n" + commandResult); // Replace with UI display logic

        // Note: If a move command was successful, the returned string from ParseCommand
        // will now contain the new location's description due to the call to GetLocationLookDescription().
        // The LocationManager's DisplayCurrentLocationInfo() is still useful for initial setup or
        // if other systems need to trigger a location display without player input.
    }
}
