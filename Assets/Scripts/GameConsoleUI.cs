using UnityEngine;
using UnityEngine.UI; // Required for ScrollRect
using TMPro;          // Required for TextMeshPro UGUI elements
using System.Collections.Generic; // Required for List
// using System.Collections; // Only if using ForceScrollDown coroutine

public class GameConsoleUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Assign the TMP_InputField for player commands here.")]
    public TMP_InputField commandInput;

    [Tooltip("Assign the TMP_Text component that will display game output.")]
    public TMP_Text outputText;

    [Tooltip("Assign the ScrollRect containing the outputText to enable scrolling.")]
    public ScrollRect outputScrollRect;

    [Header("System References")]
    [Tooltip("Assign your CommandParser GameObject/script here.")]
    public CommandParser commandParser;

    [Tooltip("Assign your LocationManager GameObject/script here.")]
    public LocationManager locationManager;

    [Header("Console Settings")]
    [Tooltip("Maximum number of lines to keep in the console output. Prevents performance issues.")]
    public int maxOutputLines = 100;
    private List<string> outputLines = new List<string>();

    // Public property to let GameManager know if the console has run its initial setup.
    public bool IsInitialized { get; private set; } = false;

    void Start()
    {
        if (commandInput == null) Debug.LogError("GameConsoleUI: Command InputField not assigned!", this);
        if (outputText == null) Debug.LogError("GameConsoleUI: Output Text not assigned!", this);
        if (outputScrollRect == null) Debug.LogWarning("GameConsoleUI: Output ScrollRect not assigned. Scrolling might not be optimal.", this);
        if (commandParser == null) Debug.LogError("GameConsoleUI: CommandParser not assigned!", this);
        if (locationManager == null) Debug.LogError("GameConsoleUI: LocationManager not assigned!", this);

        if (commandInput != null)
        {
            commandInput.onSubmit.AddListener(SubmitCommand);
            commandInput.onEndEdit.AddListener(delegate { ReFocusInputField(); });
        }
        else
        {
            Debug.LogError("GameConsoleUI: CommandInput is null, disabling script.", this);
            this.enabled = false;
        }
    }

    /// <summary>
    /// Initializes the console display. Called by GameManager when transitioning to Playing state.
    /// </summary>
    public void InitializeConsole()
    {
        if (IsInitialized)
        {
            Debug.LogWarning("GameConsoleUI: Console already initialized. Re-focusing input.", this);
            ReFocusInputField(); // If called again, just ensure focus.
            return;
        }

        if (commandParser == null || locationManager == null || outputText == null || commandInput == null)
        {
            Debug.LogError("GameConsoleUI: Cannot initialize console, critical references missing!", this);
            gameObject.SetActive(false);
            return;
        }

        Debug.Log("GameConsoleUI: Initializing console...", this);

        ClearOutput(); // This will also set IsInitialized to false initially if it did more.
        AddMessageToOutput("Welcome to your Text Adventure RPG!");
        AddMessageToOutput("Type 'help' for a list of commands.");
        AddMessageToOutput("------------------------------------");

        if (locationManager.CurrentLocation != null)
        {
            // The 'look' command in CommandParser should now correctly use the initialized player.
            string initialLocationInfo = commandParser.ParseCommand("look");
            AddMessageToOutput(initialLocationInfo);
        }
        else
        {
            AddMessageToOutput("Error: Starting location not found or LocationManager not ready!");
        }

        ReFocusInputField();
        IsInitialized = true; // Set the flag to true after successful initialization.
        Debug.Log("GameConsoleUI: Console initialized successfully.", this);
    }


    private void SubmitCommand(string rawInputText)
    {
        if (!IsInitialized)
        {
            // This might happen if the UI is somehow active before GameManager initializes it.
            Debug.LogWarning("GameConsoleUI: Console not initialized. Cannot process command yet.", this);
            // Optionally, try to initialize it here as a fallback, though GameManager should handle it.
            // InitializeConsole(); 
            // if (!IsInitialized) return; // If still not initialized, exit.
            ReFocusInputField();
            return;
        }

        if (string.IsNullOrWhiteSpace(rawInputText))
        {
            ReFocusInputField();
            return;
        }

        AddMessageToOutput("> " + rawInputText);

        if (commandParser == null)
        {
            AddMessageToOutput("Error: CommandParser not available.");
            ReFocusInputField();
            return;
        }
        string result = commandParser.ParseCommand(rawInputText);
        AddMessageToOutput(result);

        commandInput.text = "";
        ReFocusInputField();
    }

    public void AddMessageToOutput(string message)
    {
        if (outputText == null) return;

        string[] lines = message.Split('\n');
        foreach (string line in lines)
        {
            outputLines.Add(line);
        }

        while (outputLines.Count > maxOutputLines && maxOutputLines > 0)
        {
            outputLines.RemoveAt(0);
        }

        outputText.text = string.Join("\n", outputLines);
        ScrollToBottom();
    }

    private void ClearOutput()
    {
        outputLines.Clear();
        if (outputText != null) outputText.text = "";
        // IsInitialized = false; // Resetting this here could be problematic if ClearOutput is used mid-game.
        // Let InitializeConsole be the sole point for setting IsInitialized to true.
        ScrollToBottom();
    }

    private void ScrollToBottom()
    {
        if (outputScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            outputScrollRect.normalizedPosition = new Vector2(0, 0);
        }
    }

    public void ReFocusInputField() // Made public so GameManager can call it
    {
        if (commandInput != null && commandInput.gameObject.activeInHierarchy)
        {
            commandInput.ActivateInputField();
            commandInput.Select();
        }
    }

    void OnEnable()
    {
        if (IsInitialized && gameObject.activeInHierarchy) // Check if game object is active too
        {
            ReFocusInputField();
        }
    }
}
