// File: CommandParser.cs
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class CommandParser : MonoBehaviour
{
    public Player player;
    public LocationManager locationManager;

    private List<string> recognizedCommands = new List<string>()
    {
        "go [direction]", "north (n)", "south (s)", "east (e)", "west (w)",
        "look (l)",
        "inventory (inv, i)",
        "stats (stat, char)",
        "search (scan, scout)", // *** NEW COMMAND ***
        "help (?)",
        "quit (exit)"
        // Add more commands like "get [item]", "use [item]" later
    };

    void Start()
    {
        if (player == null) player = FindFirstObjectByType<Player>();
        if (locationManager == null) locationManager = FindFirstObjectByType<LocationManager>();
        if (player == null) Debug.LogError("CommandParser: Player not found!", this);
        if (locationManager == null) Debug.LogError("CommandParser: LocationManager not found!", this);
    }

    public string ParseCommand(string rawInput)
    {
        if (string.IsNullOrWhiteSpace(rawInput)) return "Please enter a command.";

        string input = rawInput.Trim().ToLower();
        string[] commandParts = input.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);

        if (commandParts.Length == 0) return "Please enter a command.";

        string verb = commandParts[0];
        string argument = commandParts.Length > 1 ? string.Join(" ", commandParts.Skip(1)) : null;

        switch (verb)
        {
            // ... (existing cases for go, n, s, e, w, look, inventory, stats, help, quit) ...
            case "go": /* ... */
                if (argument != null) { if (locationManager != null && locationManager.TryMovePlayer(argument)) return GetLocationLookDescription(); else return $"You can't go {argument}."; } else return "Go where?";
            case "north": case "n": if (locationManager != null && locationManager.TryMovePlayer("north")) return GetLocationLookDescription(); else return "You can't go north.";
            case "south": case "s": if (locationManager != null && locationManager.TryMovePlayer("south")) return GetLocationLookDescription(); else return "You can't go south.";
            case "east": case "e": if (locationManager != null && locationManager.TryMovePlayer("east")) return GetLocationLookDescription(); else return "You can't go east.";
            case "west": case "w": if (locationManager != null && locationManager.TryMovePlayer("west")) return GetLocationLookDescription(); else return "You can't go west.";
            case "look": case "l": return GetLocationLookDescription();
            case "inventory": case "inv": case "i": if (player != null) return player.GetInventoryList(); return "Player not found.";
            case "stats": case "stat": case "character": case "char": if (player != null) return player.GetPlayerStats(); return "Player not found.";
            case "help":
            case "?": /* ... help text generation ... */
                System.Text.StringBuilder helpBuilder = new System.Text.StringBuilder("Available commands:\n");
                foreach (string cmd in recognizedCommands) helpBuilder.AppendLine("- " + cmd);
                helpBuilder.AppendLine("\nType 'quit' or 'exit' to leave."); return helpBuilder.ToString();
            case "quit":
            case "exit": /* ... quit logic ... */
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
                return "Quitting...";


            // *** NEW CASE for SEARCH ***
            case "search":
            case "scan":
            case "scout":
                if (GameManager.Instance != null && locationManager != null && locationManager.CurrentLocation != null)
                {
                    // GameManager will handle the logic and return a string result
                    return GameManager.Instance.PlayerSearchesLocation();
                }
                else
                {
                    return "Cannot search right now (system error).";
                }

            default:
                return $"Unknown command: '{verb}'. Type 'help' for a list of commands.";
        }
    }

    private string GetLocationLookDescription()
    {
        if (locationManager != null && locationManager.CurrentLocation != null)
        {
            System.Text.StringBuilder lookResult = new System.Text.StringBuilder();
            lookResult.AppendLine($"Location: {locationManager.CurrentLocation.Name}");
            lookResult.AppendLine(locationManager.CurrentLocation.Description);
            lookResult.AppendLine(locationManager.CurrentLocation.GetExitsDescription());
            return lookResult.ToString();
        }
        return "You are lost in the void, or the location manager is on a break.";
    }
}