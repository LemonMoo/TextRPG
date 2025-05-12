// File: GameManager.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// Enums GameState, CombatActionType should be defined

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; private set; } = GameState.Preload;
    private GameState previousState;
    private string playerLastLocationID = "";

    [Header("System References")]
    public Player player;
    public LocationManager locationManager;
    public CommandParser commandParser;

    [Header("UI Canvases / Managers")]
    public GameObject characterCreationCanvas;
    public GameConsoleUI gameConsoleUI;
    public GameObject combatUICanvas;
    public CombatLoggerUI combatLogger;
    public PlayerStatusUI playerStatusUI;

    [Header("Game Settings")]
    public string townRespawnLocationID = "town_square";
    public float delayBetweenActions = 0.75f;
    public float delayAfterCombatMessage = 1.0f; // e.g. after "Combat Started"

    public List<Enemy> CurrentEnemiesInCombat { get; private set; }
    private List<string> slainEnemyNames = new List<string>();
    private int lastCombatTotalXPGained = 0;
    private int lastCombatGoldDropped = 0;
    private List<string> lastCombatItemNamesDropped = new List<string>();
    private bool justFinishedCombat = false;

    private Coroutine combatSequenceCoroutine;

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); } else { Destroy(gameObject); return; }
        if (player == null) player = FindFirstObjectByType<Player>();
        if (gameConsoleUI == null) gameConsoleUI = FindFirstObjectByType<GameConsoleUI>();
        if (locationManager == null) locationManager = FindFirstObjectByType<LocationManager>();
        if (commandParser == null) commandParser = FindFirstObjectByType<CommandParser>();
        if (combatLogger == null && combatUICanvas != null) combatLogger = combatUICanvas.GetComponentInChildren<CombatLoggerUI>(true);
        if (playerStatusUI == null) { GameObject hudGO = GameObject.Find("PlayerHUDCanvas"); if (hudGO != null) playerStatusUI = hudGO.GetComponent<PlayerStatusUI>(); }
        // Null checks for critical components
        if (player == null) Debug.LogError("GM Awake: Player missing!", this);
        CurrentEnemiesInCombat = new List<Enemy>();
    }

    void Start()
    {
        if (combatUICanvas != null) combatUICanvas.SetActive(false);
        if (playerStatusUI?.gameObject != null) playerStatusUI.gameObject.SetActive(false);
        ChangeState(GameState.CharacterCreation);
    }

    public void ChangeState(GameState newState) // PUBLIC
    {
        Debug.Log($"GM: ChangeState from {CurrentState} to {newState}", this);
        if (CurrentState == newState && !(newState == GameState.Combat && CurrentEnemiesInCombat.Any(e => e != null && !e.IsDefeated()))) { return; }
        if (CurrentState == GameState.Playing && newState == GameState.Combat) { playerLastLocationID = (locationManager?.CurrentLocation != null) ? locationManager.CurrentLocation.LocationID : townRespawnLocationID; }
        previousState = CurrentState; CurrentState = newState;

        characterCreationCanvas?.SetActive(false);
        combatUICanvas?.SetActive(false);
        gameConsoleUI?.gameObject.SetActive(false);
        playerStatusUI?.gameObject.SetActive(false);

        switch (CurrentState)
        {
            case GameState.CharacterCreation: characterCreationCanvas?.SetActive(true); Cursor.lockState = CursorLockMode.None; Cursor.visible = true; break;
            case GameState.Playing:
                gameConsoleUI?.gameObject.SetActive(true); if (gameConsoleUI != null && !gameConsoleUI.IsInitialized) gameConsoleUI.InitializeConsole(); else gameConsoleUI?.ReFocusInputField();
                playerStatusUI?.gameObject.SetActive(true); playerStatusUI?.TryInitializeAndRefresh();
                if (justFinishedCombat)
                { /* ... combat summary display ... */
                    gameConsoleUI?.AddMessageToOutput("--------------------");
                    if (slainEnemyNames.Any()) { gameConsoleUI.AddMessageToOutput("Enemies Slain:"); foreach (string n in slainEnemyNames) gameConsoleUI.AddMessageToOutput("- " + n); }
                    if (lastCombatItemNamesDropped.Any()) { gameConsoleUI.AddMessageToOutput("Loot Obtained:"); foreach (string i in lastCombatItemNamesDropped) gameConsoleUI.AddMessageToOutput("- " + i); }
                    if (lastCombatGoldDropped > 0) gameConsoleUI.AddMessageToOutput($"You found {lastCombatGoldDropped} gold coins.");
                    if (lastCombatTotalXPGained > 0) gameConsoleUI.AddMessageToOutput($"You gained {lastCombatTotalXPGained} experience.");
                    gameConsoleUI?.AddMessageToOutput("--------------------");
                    justFinishedCombat = false; slainEnemyNames.Clear(); lastCombatTotalXPGained = 0; lastCombatGoldDropped = 0; lastCombatItemNamesDropped.Clear();
                }
                if (locationManager?.CurrentLocation != null && commandParser != null) gameConsoleUI?.AddMessageToOutput(commandParser.ParseCommand("look"));
                break;
            case GameState.Combat:
                combatUICanvas?.SetActive(true); combatLogger?.ClearLogInstantly();
                playerStatusUI?.gameObject.SetActive(true); playerStatusUI?.TryInitializeAndRefresh();
                if (combatSequenceCoroutine != null) StopCoroutine(combatSequenceCoroutine);
                combatSequenceCoroutine = StartCoroutine(CombatStartSequence());
                break;
        }
    }

    public void FinishCharacterCreation(string playerName, PlayerRace race, PlayerClass playerClass, PlayerOrigin origin, Attributes attributes) // PUBLIC
    {
        if (player == null) return;
        player.InitializePlayer(playerName, race, playerClass, origin, attributes);
        playerStatusUI?.TryInitializeAndRefresh();
        ChangeState(GameState.Playing);
    }

    public void StartCombat(List<Enemy> enemiesToFight) // PUBLIC
    {
        if (CurrentState == GameState.Combat && CurrentEnemiesInCombat.Any(e => e != null && !e.IsDefeated())) return;
        if (enemiesToFight == null) return;
        CurrentEnemiesInCombat.Clear();
        CurrentEnemiesInCombat.AddRange(enemiesToFight.Where(e => e != null && !e.IsDefeated()));
        if (!CurrentEnemiesInCombat.Any()) { if (CurrentState == GameState.Combat) ChangeState(GameState.Playing); return; }
        ChangeState(GameState.Combat);
    }

    private IEnumerator CombatStartSequence()
    {
        LogToCombatOrGameConsole("--- COMBAT STARTED ---");
        yield return new WaitForSeconds(delayAfterCombatMessage);
        if (CurrentEnemiesInCombat.Any(e => e != null && !e.IsDefeated()))
        {
            foreach (var en in CurrentEnemiesInCombat.Where(e => e != null && !e.IsDefeated()))
            {
                LogToCombatOrGameConsole($"You face {en.Name} ({en.CurrentHealth}/{en.MaxHealth}HP)!");
                // Wait for current line to type out before showing next enemy
                while (combatLogger != null && combatLogger.IsTyping()) yield return null;
                yield return new WaitForSeconds(delayBetweenActions * 0.3f); // Shorter delay between enemy intros
            }
        }
        else
        {
            LogToCombatOrGameConsole("No active enemies."); yield return new WaitForSeconds(delayBetweenActions); EndCombat(true); yield break;
        }
        LogToCombatOrGameConsole("--- Your Turn ---");
    }

    public void ProcessPlayerCombatAction(CombatActionType action) // PUBLIC
    {
        if (CurrentState != GameState.Combat || player == null || player.CurrentHealth <= 0) { CheckCombatEnd(); return; }
        if (combatSequenceCoroutine != null) StopCoroutine(combatSequenceCoroutine);
        combatSequenceCoroutine = StartCoroutine(PlayerActionSequence(action));
    }

    private IEnumerator PlayerActionSequence(CombatActionType action)
    {
        bool consumedTurn = false;
        switch (action)
        {
            case CombatActionType.ATTACK: Enemy t = CurrentEnemiesInCombat.FirstOrDefault(e => e != null && !e.IsDefeated()); if (t != null) { yield return StartCoroutine(PlayerAttackEnemySequence(t)); consumedTurn = true; } else LogToCombatOrGameConsole("No enemies!"); break;
            case CombatActionType.CAST: LogToCombatOrGameConsole("Spells not implemented."); yield return WaitWhileTyping(); break; // Wait for msg
            case CombatActionType.ITEM: LogToCombatOrGameConsole("Items not implemented."); yield return WaitWhileTyping(); break; // Wait for msg
            case CombatActionType.FLEE: yield return StartCoroutine(AttemptFleeSequence()); consumedTurn = false; break; // Flee handles its own turn flow
        }
        if (consumedTurn && CurrentState == GameState.Combat)
        {
            yield return WaitWhileTyping(); // Wait for player action result to type
            yield return new WaitForSeconds(delayBetweenActions);
            if (player.CurrentHealth > 0 && CurrentEnemiesInCombat.Any(e => e != null && !e.IsDefeated()))
            {
                if (combatSequenceCoroutine != null) StopCoroutine(combatSequenceCoroutine);
                combatSequenceCoroutine = StartCoroutine(EnemyTurnSequence());
            }
            else CheckCombatEnd();
        }
    }

    private IEnumerator WaitWhileTyping() // Helper coroutine
    {
        if (combatLogger != null)
        {
            while (combatLogger.IsTyping()) yield return null;
        }
    }

    private IEnumerator PlayerAttackEnemySequence(Enemy target)
    {
        if (player == null || target == null) { CheckCombatEnd(); yield break; }
        int dmg = Mathf.Max(1, Random.Range(player.Attributes.Strength, player.Attributes.Strength + 5));
        LogToCombatOrGameConsole($"{player.PlayerName} attacks {target.Name} for {dmg} damage!");
        yield return WaitWhileTyping();
        target.TakeDamage(dmg);
        if (target.IsDefeated()) LogToCombatOrGameConsole($"{target.Name} defeated!");
        else LogToCombatOrGameConsole($"{target.Name} HP: {target.CurrentHealth}/{target.MaxHealth}");
        yield return WaitWhileTyping();
        CheckCombatEnd();
    }

    private IEnumerator EnemyTurnSequence()
    {
        if (CurrentState != GameState.Combat || player == null || player.CurrentHealth <= 0) { CheckCombatEnd(); yield break; }
        LogToCombatOrGameConsole("--- Enemy Turn ---");
        yield return WaitWhileTyping(); yield return new WaitForSeconds(delayBetweenActions * 0.5f);
        bool pAlive = true;
        foreach (Enemy en in CurrentEnemiesInCombat.Where(e => e != null && !e.IsDefeated()).ToList())
        {
            if (player.CurrentHealth <= 0) { pAlive = false; break; }
            LogToCombatOrGameConsole($"{en.Name} prepares..."); yield return WaitWhileTyping(); yield return new WaitForSeconds(delayBetweenActions * 0.5f);
            int dmg = en.PerformAttack(); player.TakeDamage(dmg);
            LogToCombatOrGameConsole($"{en.Name} attacks! You take {dmg} from {en.Name}! HP: {player.CurrentHealth}/{player.MaxHealth}");
            yield return WaitWhileTyping();
            if (player.CurrentHealth <= 0) { LogToCombatOrGameConsole("You are defeated!"); yield return WaitWhileTyping(); pAlive = false; break; }
            yield return new WaitForSeconds(delayBetweenActions);
        }
        CheckCombatEnd();
        if (CurrentState == GameState.Combat && pAlive) LogToCombatOrGameConsole("--- Your Turn ---");
    }

    private IEnumerator AttemptFleeSequence()
    {
        LogToCombatOrGameConsole("You attempt to flee..."); yield return WaitWhileTyping(); yield return new WaitForSeconds(delayBetweenActions);
        if (Random.Range(0f, 1f) <= 0.75f) { LogToCombatOrGameConsole("...successfully escaped!"); yield return WaitWhileTyping(); yield return new WaitForSeconds(delayAfterCombatMessage); EndCombat(false); }
        else
        {
            LogToCombatOrGameConsole("...but fail!"); yield return WaitWhileTyping(); yield return new WaitForSeconds(delayBetweenActions);
            if (CurrentState == GameState.Combat) { if (combatSequenceCoroutine != null) StopCoroutine(combatSequenceCoroutine); combatSequenceCoroutine = StartCoroutine(EnemyTurnSequence()); }
        }
    }

    public void EndCombat(bool playerWon) // PUBLIC
    {
        Debug.Log($"GM: EndCombat. Won: {playerWon}.", this);
        if (combatSequenceCoroutine != null) { StopCoroutine(combatSequenceCoroutine); combatSequenceCoroutine = null; }
        slainEnemyNames.Clear(); lastCombatTotalXPGained = 0; lastCombatGoldDropped = 0; lastCombatItemNamesDropped.Clear();
        justFinishedCombat = true;
        combatUICanvas?.SetActive(false);
        // if (CurrentState != GameState.Combat && playerWon) { Debug.LogWarning("EndCombat: Won but not in Combat."); } // Simplified
        // else if (CurrentState != GameState.Combat && !playerWon) { Debug.Log("EndCombat: Lost/Fled and not in Combat."); }

        string immediateLogMsg;
        if (playerWon)
        {
            int goldThisFight = 0; List<Item> itemsThisFight = new List<Item>();
            foreach (Enemy en in CurrentEnemiesInCombat) if (en != null && en.IsDefeated())
                {
                    lastCombatTotalXPGained += en.ExperienceReward; slainEnemyNames.Add(en.Name);
                    int goldFromEn; List<Item> itemsFromEn = en.GetDroppedLoot(out goldFromEn);
                    if (goldFromEn > 0) goldThisFight += goldFromEn; if (itemsFromEn.Any()) itemsThisFight.AddRange(itemsFromEn);
                }
            lastCombatGoldDropped = goldThisFight; // This is where goldThisFight is used
            lastCombatItemNamesDropped = itemsThisFight.Select(item => item.Name + (item.IsStackable && item.Quantity > 1 ? $" (x{item.Quantity})" : "")).ToList();
            if (player != null) { player.GainExperience(lastCombatTotalXPGained); if (goldThisFight > 0) player.AddGold(goldThisFight); foreach (Item itm in itemsThisFight) player.AddItemToInventory(itm); }
            immediateLogMsg = "You are victorious!";
            if (!string.IsNullOrEmpty(playerLastLocationID) && locationManager != null) { if (!locationManager.SetCurrentLocation(playerLastLocationID)) locationManager.SetCurrentLocation(townRespawnLocationID); }
            else { if (locationManager != null) locationManager.SetCurrentLocation(townRespawnLocationID); }
        }
        else
        {
            if (player != null && player.CurrentHealth <= 0) { immediateLogMsg = "You have been defeated..."; player.RestoreToMaxStats(); if (locationManager != null) if (!locationManager.SetCurrentLocation(townRespawnLocationID)) LogToCombatOrGameConsole("Could not respawn.", true); }
            else { immediateLogMsg = "Combat ended."; }
        }
        LogToCombatOrGameConsole(immediateLogMsg, true); LogToCombatOrGameConsole("--- COMBAT ENDED ---", true);
        CurrentEnemiesInCombat.Clear();
        ChangeState(GameState.Playing);
    }

    private void CheckCombatEnd()
    {
        if (CurrentState != GameState.Combat) return;
        bool pDefeated = player != null && player.CurrentHealth <= 0;
        bool activeEnemies = CurrentEnemiesInCombat.Any(e => e != null && !e.IsDefeated());
        if (pDefeated) { if (combatSequenceCoroutine != null) StopCoroutine(combatSequenceCoroutine); EndCombat(false); }
        else if (!activeEnemies && CurrentEnemiesInCombat.Any()) { if (combatSequenceCoroutine != null) StopCoroutine(combatSequenceCoroutine); EndCombat(true); }
    }

    public void TryToTriggerEncounter(Location location) // PUBLIC
    {
        if (location == null || CurrentState != GameState.Playing) return;
        List<Enemy> encounterPack = location.GetEncounterPack();
        if (encounterPack.Any())
        {
            // Log ambush message to main console BEFORE state change to Combat
            if (gameConsoleUI?.gameObject.activeInHierarchy == true) gameConsoleUI.AddMessageToOutput($"You are ambushed in {location.Name}!");
            else Debug.Log($"Ambushed in {location.Name}!"); // Fallback log
            StartCombat(encounterPack);
        }
    }

    private void LogToCombatOrGameConsole(string message, bool combatLogPriority = false)
    {
        if (CurrentState == GameState.Combat && combatLogger != null)
        {
            combatLogger.AddMessage(message);
        }
        else if (gameConsoleUI?.gameObject.activeInHierarchy == true)
        {
            gameConsoleUI.AddMessageToOutput(message);
        }
        else if (gameConsoleUI != null)
        {
            gameConsoleUI.AddMessageToOutput(message); Debug.Log($"GameConsoleUI (inactive): {message}", this);
        }
        else { Debug.Log($"General Log (No UI): {message}", this); }
    }
    public void SendMessageToPlayerLog(string message) { LogToCombatOrGameConsole(message, false); } // PUBLIC
}