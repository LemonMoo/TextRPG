using UnityEngine;
using System.Collections.Generic; // Required for List of enemies

public enum GameState
{
    CharacterCreation,
    Playing,
    Combat,
    Paused
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; private set; }
    private GameState previousState;

    [Header("System References")]
    public Player player;
    public LocationManager locationManager;
    public CommandParser commandParser; // Added reference for convenience

    [Header("UI Canvases / Managers")]
    public GameObject characterCreationCanvas;
    public GameConsoleUI gameConsoleUI;
    public GameObject combatUICanvas;

    public List<Enemy> CurrentEnemiesInCombat { get; private set; }

    void Awake()
    {
        Debug.Log("GameManager: Awake() called.", this);
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (player == null)
        {
            player = FindFirstObjectByType<Player>();
            if (player == null) Debug.LogError("GameManager: Player not found in scene and not assigned!", this);
            else Debug.Log("GameManager: Player found via FindFirstObjectByType.", this);
        }
        if (gameConsoleUI == null)
        {
            gameConsoleUI = FindFirstObjectByType<GameConsoleUI>();
            if (gameConsoleUI == null) Debug.LogError("GameManager: GameConsoleUI not found in scene and not assigned!", this);
            else Debug.Log("GameManager: GameConsoleUI found via FindFirstObjectByType.", this);
        }
        if (locationManager == null)
        {
            locationManager = FindFirstObjectByType<LocationManager>();
            if (locationManager == null) Debug.LogError("GameManager: LocationManager not found in scene and not assigned!", this);
            else Debug.Log("GameManager: LocationManager found via FindFirstObjectByType.", this);
        }
        if (commandParser == null)
        {
            commandParser = FindFirstObjectByType<CommandParser>();
            if (commandParser == null) Debug.LogError("GameManager: CommandParser not found in scene and not assigned!", this);
            else Debug.Log("GameManager: CommandParser found via FindFirstObjectByType.", this);
        }

        CurrentEnemiesInCombat = new List<Enemy>();
        Debug.Log("GameManager: Awake() completed. All references checked.", this);
    }

    void Start()
    {
        Debug.Log("GameManager: Start() called.", this);
        if (combatUICanvas != null) combatUICanvas.SetActive(false);
        else Debug.LogWarning("GameManager: Combat UI Canvas is not assigned. Combat state will not show specific UI.", this);

        ChangeState(GameState.CharacterCreation);
    }

    public void ChangeState(GameState newState)
    {
        Debug.Log($"GameManager: Attempting to change state from {CurrentState} to {newState}", this);
        if (CurrentState == newState && newState != GameState.Combat && CurrentEnemiesInCombat.Count == 0)
        {
            Debug.Log($"GameManager: State change to {newState} aborted (already in this state or not applicable).", this);
            return;
        }

        previousState = CurrentState;
        CurrentState = newState;
        Debug.Log($"GameManager: State changed successfully from {previousState} to {CurrentState}", this);

        // Log current state of UI objects before changing them
        Debug.Log($"GameManager: Before UI switch - CC_Canvas Active: {(characterCreationCanvas != null ? characterCreationCanvas.activeSelf.ToString() : "N/A")}, GameConsoleUI Active: {(gameConsoleUI != null && gameConsoleUI.gameObject != null ? gameConsoleUI.gameObject.activeSelf.ToString() : "N/A")}", this);

        if (characterCreationCanvas != null) characterCreationCanvas.SetActive(false);
        if (gameConsoleUI != null && gameConsoleUI.gameObject != null) gameConsoleUI.gameObject.SetActive(false);
        if (combatUICanvas != null) combatUICanvas.SetActive(false);
        Debug.Log("GameManager: All primary UI canvases/GameObjects deactivated.", this);


        switch (CurrentState)
        {
            case GameState.CharacterCreation:
                Debug.Log("GameManager: Setting up CharacterCreation state.", this);
                if (characterCreationCanvas != null)
                {
                    characterCreationCanvas.SetActive(true);
                    Debug.Log($"GameManager: CharacterCreationCanvas activated. ActiveSelf: {characterCreationCanvas.activeSelf}", this);
                }
                else Debug.LogError("GameManager: characterCreationCanvas is NULL when trying to activate for CharacterCreation state!", this);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;

            case GameState.Playing:
                Debug.Log("GameManager: Setting up Playing state.", this);
                if (gameConsoleUI != null && gameConsoleUI.gameObject != null)
                {
                    gameConsoleUI.gameObject.SetActive(true);
                    Debug.Log($"GameManager: gameConsoleUI.gameObject activated. ActiveSelf: {gameConsoleUI.gameObject.activeSelf}", this);

                    if (!gameConsoleUI.IsInitialized)
                    {
                        Debug.Log("GameManager: GameConsoleUI is not initialized. Calling InitializeConsole().", this);
                        gameConsoleUI.InitializeConsole();
                    }
                    else
                    {
                        Debug.Log("GameManager: GameConsoleUI is already initialized. Calling ReFocusInputField().", this);
                        gameConsoleUI.ReFocusInputField();
                        if (previousState == GameState.Combat && locationManager != null && locationManager.CurrentLocation != null)
                        {
                            if (commandParser != null)
                            {
                                Debug.Log("GameManager: Returning from combat. Displaying current location.", this);
                                gameConsoleUI.AddMessageToOutput(commandParser.ParseCommand("look"));
                            }
                            else Debug.LogWarning("GameManager: CommandParser is null, cannot display location after combat.", this);
                        }
                    }
                }
                else Debug.LogError("GameManager: gameConsoleUI or gameConsoleUI.gameObject is NULL when trying to activate for Playing state!", this);
                break;

            case GameState.Combat:
                Debug.Log("GameManager: Setting up Combat state.", this);
                if (combatUICanvas != null)
                {
                    combatUICanvas.SetActive(true);
                    Debug.Log($"GameManager: CombatUICanvas activated. ActiveSelf: {combatUICanvas.activeSelf}", this);
                }
                else Debug.LogWarning("GameManager: combatUICanvas is NULL. Combat will proceed without dedicated UI.", this);

                if (gameConsoleUI != null)
                {
                    gameConsoleUI.AddMessageToOutput("--- COMBAT STARTED ---");
                    foreach (var enemy in CurrentEnemiesInCombat)
                    {
                        gameConsoleUI.AddMessageToOutput($"You face a {enemy.Name} ({enemy.CurrentHealth}/{enemy.MaxHealth} HP)!");
                    }
                }
                break;

            case GameState.Paused:
                Debug.Log("GameManager: Setting up Paused state.", this);
                break;
        }
        Debug.Log($"GameManager: ChangeState to {CurrentState} completed.", this);
    }

    public void FinishCharacterCreation(string playerName, PlayerRace race, PlayerClass playerClass, PlayerOrigin origin, Attributes attributes)
    {
        Debug.Log($"GameManager: FinishCharacterCreation called. Name: {playerName}, Race: {race}, Class: {playerClass}, Origin: {origin}", this);
        if (player == null)
        {
            Debug.LogError("GameManager: Player reference is NULL in FinishCharacterCreation. Cannot initialize player. Aborting transition to Playing state.", this);
            return;
        }
        player.InitializePlayer(playerName, race, playerClass, origin, attributes);
        Debug.Log("GameManager: Player initialized. Attempting to change state to Playing.", this);
        ChangeState(GameState.Playing);
    }

    public void StartCombat(List<Enemy> enemiesToFight)
    {
        Debug.Log("GameManager: StartCombat called.", this);
        if (CurrentState == GameState.Combat)
        {
            Debug.LogWarning("GameManager: StartCombat called while already in combat. Ignoring.", this);
            return;
        }
        if (enemiesToFight == null || enemiesToFight.Count == 0)
        {
            Debug.LogWarning("GameManager: StartCombat called with no enemies. No combat initiated.", this);
            return;
        }

        CurrentEnemiesInCombat.Clear();
        CurrentEnemiesInCombat.AddRange(enemiesToFight);

        Debug.Log($"GameManager: Starting combat with {enemiesToFight.Count} enemies. First enemy: {enemiesToFight[0].Name}", this);
        ChangeState(GameState.Combat);
    }

    public void EndCombat(bool playerWon)
    {
        Debug.Log($"GameManager: EndCombat called. PlayerWon: {playerWon}", this);
        if (CurrentState != GameState.Combat)
        {
            Debug.LogWarning("GameManager: EndCombat called when not in combat state. Current state: " + CurrentState, this);
            ChangeState(GameState.Playing);
            return;
        }

        string combatEndMessage;
        if (playerWon)
        {
            int totalExpGained = 0;
            foreach (Enemy enemy in CurrentEnemiesInCombat)
            {
                if (enemy.IsDefeated())
                {
                    totalExpGained += enemy.ExperienceReward;
                }
            }
            if (player != null)
            {
                player.GainExperience(totalExpGained);
            }
            else Debug.LogError("GameManager: Player is NULL in EndCombat. Cannot award experience.", this);
            combatEndMessage = $"You are victorious! You gained {totalExpGained} experience.";
        }
        else
        {
            combatEndMessage = "You have been defeated...";
        }

        if (gameConsoleUI != null)
        {
            gameConsoleUI.AddMessageToOutput(combatEndMessage);
            gameConsoleUI.AddMessageToOutput("--- COMBAT ENDED ---");
        }

        CurrentEnemiesInCombat.Clear();
        GameState targetState = (previousState == GameState.Combat || previousState == GameState.CharacterCreation) ? GameState.Playing : previousState;
        Debug.Log($"GameManager: Combat ended. Transitioning to state: {targetState}", this);
        ChangeState(targetState);
    }

    public void TryToTriggerEncounter(Location location)
    {
        if (location == null)
        {
            Debug.LogWarning("GameManager: TryToTriggerEncounter called with null location.", this);
            return;
        }
        Debug.Log($"GameManager: TryToTriggerEncounter called for location: {location.Name}. Current state: {CurrentState}", this);
        if (CurrentState != GameState.Playing)
        {
            Debug.Log("GameManager: Not in Playing state, encounter check skipped.", this);
            return;
        }

        List<Enemy> encounterPack = location.GetEncounterPack();
        if (encounterPack.Count > 0)
        {
            Debug.Log($"GameManager: Encounter triggered in {location.Name}! {encounterPack.Count} enemies.", this);
            if (gameConsoleUI != null)
            {
                gameConsoleUI.AddMessageToOutput($"You are ambushed by enemies in {location.Name}!");
            }
            StartCombat(encounterPack);
        }
        else
        {
            Debug.Log($"GameManager: No encounter triggered in {location.Name} (either no pack or chance failed).", this);
        }
    }
}
