// File: GameManager.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// Enums GameState, CombatActionType, CombatMenuState, FighterSkillType should be defined (e.g., in their own .cs files)

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
    public CombatMenuUI combatMenuUI;
    public EnemyStatusUI enemyStatusUI;

    [Header("Game Settings")]
    public string townRespawnLocationID = "town_square";
    public float delayBetweenActions = 0.65f;
    public float delayAfterMajorMessage = 0.8f;
    public float delayBetweenEnemyAttacks = 0.4f;
    public float delayBeforeEnemyAttack = 0.3f;

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
        if (combatUICanvas != null)
        {
            if (combatLogger == null) combatLogger = combatUICanvas.GetComponentInChildren<CombatLoggerUI>(true);
            if (combatMenuUI == null) combatMenuUI = combatUICanvas.GetComponentInChildren<CombatMenuUI>(true);
            if (enemyStatusUI == null) { enemyStatusUI = combatUICanvas.GetComponentInChildren<EnemyStatusUI>(true); }
        }
        if (enemyStatusUI == null) Debug.LogWarning("GM Awake: EnemyStatusUI missing. Enemy HUD will not display.", this);
        if (playerStatusUI == null) { GameObject hudGO = GameObject.Find("PlayerHUDCanvas"); if (hudGO != null) playerStatusUI = hudGO.GetComponent<PlayerStatusUI>(); }

        if (player == null) Debug.LogError("GM Awake: Player missing!", this);
        if (combatMenuUI == null) Debug.LogWarning("GM Awake: CombatMenuUI missing.", this);
        CurrentEnemiesInCombat = new List<Enemy>();
    }

    void Start()
    {
        combatUICanvas?.SetActive(false);
        playerStatusUI?.gameObject.SetActive(false);
        ChangeState(GameState.CharacterCreation);
    }

    public void ChangeState(GameState newState)
    {
        Debug.Log($"GM: ChangeState from {CurrentState} to {newState}", this);
        if (CurrentState == newState && !(newState == GameState.Combat && CurrentEnemiesInCombat.Any(e => e != null && !e.IsDefeated()))) { return; }
        if (CurrentState == GameState.Playing && newState == GameState.Combat) { playerLastLocationID = (locationManager?.CurrentLocation != null) ? locationManager.CurrentLocation.LocationID : townRespawnLocationID; }
        previousState = CurrentState; CurrentState = newState;

        characterCreationCanvas?.SetActive(false);
        combatUICanvas?.SetActive(false);
        gameConsoleUI?.gameObject.SetActive(false);
        playerStatusUI?.gameObject.SetActive(false);
        enemyStatusUI?.gameObject.SetActive(false);
        combatMenuUI?.DisableInput();

        switch (CurrentState)
        {
            case GameState.CharacterCreation: characterCreationCanvas?.SetActive(true); Cursor.lockState = CursorLockMode.None; Cursor.visible = true; break;
            case GameState.Playing:
                gameConsoleUI?.gameObject.SetActive(true); if (gameConsoleUI != null && !gameConsoleUI.IsInitialized) gameConsoleUI.InitializeConsole(); else gameConsoleUI?.ReFocusInputField();
                playerStatusUI?.gameObject.SetActive(true); playerStatusUI?.TryInitializeAndRefresh();
                enemyStatusUI?.HideDisplay();
                if (justFinishedCombat)
                {
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
                combatMenuUI?.TransitionToState(CombatMenuState.Main);
                if (enemyStatusUI != null)
                {
                    if (CurrentEnemiesInCombat.Any(e => e != null && !e.IsDefeated()))
                    {
                        // For now, target the first active enemy. Future: implement targeting.
                        Enemy target = CurrentEnemiesInCombat.First(e => e != null && !e.IsDefeated());
                        enemyStatusUI.SetTargetEnemy(target);
                        // enemyStatusUI.gameObject.SetActive(true); // SetTargetEnemy handles this
                    }
                    else
                    {
                        enemyStatusUI.HideDisplay(); // No valid enemies to display
                    }
                }

                if (combatSequenceCoroutine != null) StopCoroutine(combatSequenceCoroutine);
                combatSequenceCoroutine = StartCoroutine(CombatStartSequence());
                break;
        }   
    }

    public void FinishCharacterCreation(string playerName, PlayerRace race, PlayerClass playerClass, PlayerOrigin origin, Attributes attributes)
    {
        if (player == null) { Debug.LogError("GM FinishCC: Player is null!", this); return; }
        player.InitializePlayer(playerName, race, playerClass, origin, attributes);
        playerStatusUI?.TryInitializeAndRefresh();
        ChangeState(GameState.Playing);
    }

    public void StartCombat(List<Enemy> enemiesToFight)
    {
        if (CurrentState == GameState.Combat && CurrentEnemiesInCombat.Any(e => e != null && !e.IsDefeated())) return;
        if (enemiesToFight == null) return;
        CurrentEnemiesInCombat.Clear();
        CurrentEnemiesInCombat.AddRange(enemiesToFight.Where(e => e != null && !e.IsDefeated()));
        if (!CurrentEnemiesInCombat.Any()) { if (CurrentState == GameState.Combat) ChangeState(GameState.Playing); return; }
        ChangeState(GameState.Combat);
    }

    public void EndCombat(bool playerWon)
    {
        Debug.Log($"GM: EndCombat. Won: {playerWon}.", this);
        if (combatSequenceCoroutine != null) { StopCoroutine(combatSequenceCoroutine); combatSequenceCoroutine = null; }
        combatMenuUI?.DisableInput(); combatMenuUI?.TransitionToState(CombatMenuState.Main);
        slainEnemyNames.Clear(); lastCombatTotalXPGained = 0; lastCombatGoldDropped = 0; lastCombatItemNamesDropped.Clear(); enemyStatusUI?.HideDisplay();
        justFinishedCombat = true;

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
            lastCombatGoldDropped = goldThisFight;
            lastCombatItemNamesDropped = itemsThisFight.Select(item => item.Name + (item.IsStackable && item.Quantity > 1 ? $" (x{item.Quantity})" : "")).ToList();
            if (player != null) { player.GainExperience(lastCombatTotalXPGained); if (goldThisFight > 0) player.AddGold(goldThisFight); foreach (Item itm in itemsThisFight) player.AddItemToInventory(itm); }
            immediateLogMsg = "You are victorious!";
            if (!string.IsNullOrEmpty(playerLastLocationID) && locationManager != null) { if (!locationManager.SetCurrentLocation(playerLastLocationID)) locationManager.SetCurrentLocation(townRespawnLocationID); }
            else { if (locationManager != null) locationManager.SetCurrentLocation(townRespawnLocationID); }
        }
        else
        {
            if (player != null && player.CurrentHealth <= 0) { immediateLogMsg = "You have been defeated..."; player.RestoreToMaxStats(); if (locationManager != null) if (!locationManager.SetCurrentLocation(townRespawnLocationID)) LogToCombatOrGameConsole("Could not respawn.", true); }
            else { immediateLogMsg = "Combat ended."; } // Player Fled
        }
        // These final messages should ideally also wait for typing if CombatLogger is still active.
        // However, ChangeState will immediately hide it. A small delay before ChangeState could work,
        // or these specific messages could be passed to the GameConsoleUI in Playing state.
        LogToCombatOrGameConsole(immediateLogMsg, true);
        LogToCombatOrGameConsole("--- COMBAT ENDED ---", true);
        // Consider: StartCoroutine(DelayedEndCombatTransition(delayAfterMajorMessage));
        CurrentEnemiesInCombat.Clear();
        ChangeState(GameState.Playing);
    }

    public void ProcessPlayerCombatAction(CombatActionType action)
    {
        if (CurrentState != GameState.Combat || player == null || player.CurrentHealth <= 0) { CheckCombatEnd(); return; }
        if (combatSequenceCoroutine != null) StopCoroutine(combatSequenceCoroutine);
        combatSequenceCoroutine = StartCoroutine(PlayerActionSequence(action));
    }

    public void ProcessPlayerFighterSkill(FighterSkillType skill)
    {
        if (CurrentState != GameState.Combat || player == null || player.Class != PlayerClass.Fighter || player.CurrentHealth <= 0)
        {
            combatMenuUI?.TransitionToState(CombatMenuState.Main); combatMenuUI?.EnableInput(); return;
        }
        if (combatSequenceCoroutine != null) StopCoroutine(combatSequenceCoroutine);
        combatSequenceCoroutine = StartCoroutine(PlayerActionSequence_FighterSkill(skill));
    }

    public void TryToTriggerEncounter(Location location)
    {
        if (location == null || CurrentState != GameState.Playing) return;
        List<Enemy> encounterPack = location.GetEncounterPack();
        if (encounterPack.Any())
        {
            if (gameConsoleUI?.gameObject.activeInHierarchy == true) gameConsoleUI.AddMessageToOutput($"You are ambushed in {location.Name}!");
            else Debug.Log($"Ambushed in {location.Name}!");
            StartCombat(encounterPack);
        }
        PerformEncounterCheck(location, true); // true for "isAmbush"
    }

    public void SendMessageToPlayerLog(string message)
    {
        // Route player messages through the standard logger.
        // Combat log priority false means it will prefer GameConsoleUI if not in combat.
        LogToCombatOrGameConsole(message, false);
    }

    public string PlayerSearchesLocation()
    {
        if (CurrentState != GameState.Playing)
        {
            return "You can only search when exploring.";
        }
        if (locationManager == null || locationManager.CurrentLocation == null)
        {
            return "You're not sure where you are to search effectively.";
        }

        Location currentLocation = locationManager.CurrentLocation;
        LogToCombatOrGameConsole("You begin searching the area carefully..."); // Message to GameConsoleUI

        // Start a coroutine to handle the search result after a delay
        // This prevents combat from starting in the same frame as the search command,
        // allowing the "searching..." message to display first.
        if (combatSequenceCoroutine != null) StopCoroutine(combatSequenceCoroutine); // Stop any other pending sequences
        combatSequenceCoroutine = StartCoroutine(SearchLocationSequence(currentLocation));

        return ""; // The coroutine will handle further messages/outcomes. CommandParser gets an empty string.
                   // GameConsoleUI will still show the "searching..." message via LogToCombatOrGameConsole.
    }

    private void CheckCombatEnd()
    {
        if (CurrentState != GameState.Combat) return;
        bool pDefeated = player != null && player.CurrentHealth <= 0;
        bool activeEnemies = CurrentEnemiesInCombat.Any(e => e != null && !e.IsDefeated());
        if (pDefeated) { if (combatSequenceCoroutine != null) StopCoroutine(combatSequenceCoroutine); EndCombat(false); }
        else if (!activeEnemies && CurrentEnemiesInCombat.Any()) { if (combatSequenceCoroutine != null) StopCoroutine(combatSequenceCoroutine); EndCombat(true); }
    }

    private void LogToCombatOrGameConsole(string message, bool combatLogPriority = false)
    {
        if (CurrentState == GameState.Combat && combatLogger != null && combatUICanvas.activeInHierarchy)
        { // Ensure combat UI is active
            combatLogger.AddMessage(message);
        }
        else if (gameConsoleUI?.gameObject.activeInHierarchy == true)
        {
            gameConsoleUI.AddMessageToOutput(message);
        }
        else if (gameConsoleUI != null)
        { // Log to it even if inactive for queueing
            gameConsoleUI.AddMessageToOutput(message);
        }
        else { Debug.Log($"General Log (No UI Target): {message}", this); }
    }

    private bool PerformEncounterCheck(Location location, bool isAmbush)
    {
        if (location == null) return false;
        // Debug.Log($"GM: Performing encounter check for {location.Name}. IsAmbush: {isAmbush}", this);

        List<Enemy> encounterPack = location.GetEncounterPack(); // This uses location's EncounterChance

        if (encounterPack.Any())
        {
            if (isAmbush)
            {
                // Log to main console UI as player is in Playing state when ambushed
                if (gameConsoleUI?.gameObject.activeInHierarchy == true)
                    gameConsoleUI.AddMessageToOutput($"You are ambushed in {location.Name}!");
                else
                    Debug.Log($"Ambushed in {location.Name}!"); // Fallback
            }
            else // Player initiated search and found something
            {
                // Log to main console UI
                if (gameConsoleUI?.gameObject.activeInHierarchy == true)
                    gameConsoleUI.AddMessageToOutput($"Your search reveals danger! You've found {encounterPack.First().Name}!");
                else
                    Debug.Log($"Search found {encounterPack.First().Name}!");
            }
            StartCombat(encounterPack);
            return true; // Combat started
        }
        return false; // No combat started
    }

    private IEnumerator CombatStartSequence()
    {
        LogToCombatOrGameConsole("--- COMBAT STARTED ---");
        yield return StartCoroutine(WaitWhileTypingAndDelay(delayAfterMajorMessage));
        if (CurrentEnemiesInCombat.Any(e => e != null && !e.IsDefeated()))
        {
            foreach (var en in CurrentEnemiesInCombat.Where(e => e != null && !e.IsDefeated()))
            {
                LogToCombatOrGameConsole($"You face {en.Name} ({en.CurrentHealth}/{en.MaxHealth}HP)!");
                yield return StartCoroutine(WaitWhileTypingAndDelay(delayBetweenEnemyAttacks * 0.8f));
            }
        }
        else
        {
            LogToCombatOrGameConsole("No active enemies at the start of combat.");
            yield return StartCoroutine(WaitWhileTypingAndDelay(delayBetweenActions));
            EndCombat(true); yield break;
        }
        LogToCombatOrGameConsole("--- Your Turn ---");
        yield return StartCoroutine(WaitWhileTypingAndDelay(0.1f));
        combatMenuUI?.EnableInput();
    }

    private IEnumerator PlayerActionSequence(CombatActionType action)
    {
        bool consumedTurn = false;
        switch (action)
        {
            case CombatActionType.ATTACK: Enemy t = CurrentEnemiesInCombat.FirstOrDefault(e => e != null && !e.IsDefeated()); if (t != null) { yield return StartCoroutine(PlayerAttackEnemySequence(t)); consumedTurn = true; } else { LogToCombatOrGameConsole("No valid targets!"); yield return StartCoroutine(WaitWhileTypingAndDelay(0f)); } break;
            case CombatActionType.CAST: LogToCombatOrGameConsole("No spells available or usable."); yield return StartCoroutine(WaitWhileTypingAndDelay(0f)); break;
            case CombatActionType.ITEM: LogToCombatOrGameConsole("Using items is not yet implemented."); yield return StartCoroutine(WaitWhileTypingAndDelay(0f)); break;
            case CombatActionType.FLEE: yield return StartCoroutine(AttemptFleeSequence()); consumedTurn = false; break;
        }
        if (consumedTurn && CurrentState == GameState.Combat)
        {
            yield return StartCoroutine(WaitWhileTypingAndDelay(delayBetweenActions)); if (player.CurrentHealth > 0 && CurrentEnemiesInCombat.Any(e => e != null && !e.IsDefeated())) { if (combatSequenceCoroutine != null) StopCoroutine(combatSequenceCoroutine); combatSequenceCoroutine = StartCoroutine(EnemyTurnSequence()); } else CheckCombatEnd();
        }
        else if (CurrentState == GameState.Combat && !consumedTurn && (action == CombatActionType.CAST || action == CombatActionType.ITEM))
        {
            LogToCombatOrGameConsole("Choose your action."); yield return StartCoroutine(WaitWhileTypingAndDelay(0.1f));
            combatMenuUI?.TransitionToState(CombatMenuState.Main); combatMenuUI?.EnableInput();
        }
    }

    private IEnumerator PlayerActionSequence_FighterSkill(FighterSkillType skill)
    {
        bool turnConsumedAndSuccessful = false; // More specific flag       

        switch (skill)
        {
            case FighterSkillType.Counterattack:
                if (player.ActivateCounterattack()) // ActivateCounterattack logs its own success/failure message
                {
                    turnConsumedAndSuccessful = true;
                }
                // If ActivateCounterattack() returned false, it means not enough rage or already active.
                // It would have logged this itself via Player.cs -> LogPlayerMessage.
                break;
                // Future: case FighterSkillType.PowerStrike: ... break;
        }

        // Wait for any message from player.ActivateCounterattack() to finish typing
        yield return StartCoroutine(WaitWhileTypingAndDelay(0f));

        if (turnConsumedAndSuccessful && CurrentState == GameState.Combat)
        {
            // Skill was successful and consumed the turn.
            // Visually, player should see they are no longer making choices.
            combatMenuUI?.TransitionToState(CombatMenuState.Main); // Ensure main menu is "visible" layer, even if input is off.
            combatMenuUI?.DisableInput(); // Explicitly ensure menu input is off before enemy turn.

            LogToCombatOrGameConsole($"Player used {skill}. Ending player's turn."); // Optional log
            yield return StartCoroutine(WaitWhileTypingAndDelay(delayBetweenActions)); // Pause after player's skill

            // Check combat state again before proceeding to enemy turn
            if (player.CurrentHealth > 0 && CurrentEnemiesInCombat.Any(e => e != null && !e.IsDefeated()))
            {
                if (combatSequenceCoroutine != null) StopCoroutine(combatSequenceCoroutine); // Stop self
                combatSequenceCoroutine = StartCoroutine(EnemyTurnSequence());
            }
            else
            {
                CheckCombatEnd(); // Skill might have somehow ended combat (unlikely for Counterattack setup)
            }
        }
        else if (CurrentState == GameState.Combat && !turnConsumedAndSuccessful)
        {
            // Skill was attempted but failed (e.g., not enough rage, already active).
            // Player should be able to choose another action from the SKILL MENU or go BACK.
            // CombatMenuUI should still be in CombatMenuState.Skills.
            LogToCombatOrGameConsole("Skill could not be used. Choose another skill or go 'BACK'.");
            yield return StartCoroutine(WaitWhileTypingAndDelay(0.1f));
            combatMenuUI?.EnableInput(); // Re-enable input for the SKILL MENU
        }
        // If combat ended for some reason, do nothing more here, CheckCombatEnd/EndCombat handles it.
    }

    private IEnumerator WaitWhileTypingAndDelay(float additionalDelay)
    {
        // Only wait for typing if we are in combat and the combat logger is active and has an IsTyping method
        if (CurrentState == GameState.Combat && combatLogger != null && combatLogger.gameObject.activeInHierarchy)
        {
            // Assuming combatLogger is of type CombatLoggerUI and has IsTyping()
            while (combatLogger.IsTyping())
            {
                yield return null;
            }
        }
        // No general check for GameConsoleUI.IsTyping() as it doesn't have that feature.

        // Apply the additional delay regardless of typing
        if (additionalDelay > 0f)
        {
            yield return new WaitForSeconds(additionalDelay);
        }
    }

    private IEnumerator PlayerAttackEnemySequence(Enemy target)
    {
        if (player == null || target == null) { CheckCombatEnd(); yield break; }
        int dmg = player.GetBasicAttackDamage();
        LogToCombatOrGameConsole($"{player.PlayerName} attacks {target.Name} for {dmg} damage!");
        yield return StartCoroutine(WaitWhileTypingAndDelay(0f));
        target.TakeDamage(dmg);
        enemyStatusUI?.UpdateDisplay();
        if (player.Class == PlayerClass.Fighter && player.MaxRage > 0)
        {
            int rageGained = Mathf.Clamp(dmg / 2, 2, 10); player.GenerateRage(rageGained);
            // Player.GenerateRage can optionally log itself via LogPlayerMessage if rage changes significantly
            // For explicit message: LogToCombatOrGameConsole($"Attack generates {rageGained} Rage! ({player.CurrentRage}/{player.MaxRage})");
            // For now, Player.TakeDamage for player-hit rage, and this for player-dealt rage for fighters.
            if (rageGained > 0) LogToCombatOrGameConsole($"Your strikes build {rageGained} Rage. ({player.CurrentRage}/{player.MaxRage})");
            yield return StartCoroutine(WaitWhileTypingAndDelay(0f));
        }
        if (target.IsDefeated()) LogToCombatOrGameConsole($"{target.Name} has been defeated!");
        else LogToCombatOrGameConsole($"{target.Name} has {target.CurrentHealth}/{target.MaxHealth} HP remaining.");
        yield return StartCoroutine(WaitWhileTypingAndDelay(0f));
        CheckCombatEnd();
    }

    private IEnumerator EnemyTurnSequence()
    {
        if (CurrentState != GameState.Combat || player == null || player.CurrentHealth <= 0) { CheckCombatEnd(); yield break; }
        LogToCombatOrGameConsole("--- Enemy Turn ---");
        yield return StartCoroutine(WaitWhileTypingAndDelay(delayBetweenActions * 0.5f));
        bool pStillAlive = true;
        foreach (Enemy en in CurrentEnemiesInCombat.Where(e => e != null && !e.IsDefeated()).ToList())
        {
            if (player.CurrentHealth <= 0) { pStillAlive = false; break; }
            LogToCombatOrGameConsole($"{en.Name} prepares to strike...");
            yield return StartCoroutine(WaitWhileTypingAndDelay(delayBeforeEnemyAttack));
            int enemyDamage = en.PerformAttack();
            if (player.IsCounterattackActive)
            {
                LogToCombatOrGameConsole($"{player.PlayerName} PARRIES {en.Name}'s attack with Counterattack!"); yield return StartCoroutine(WaitWhileTypingAndDelay(0f));
                int counterDmg = Mathf.RoundToInt(player.GetBasicAttackDamage() * 1.1f);
                LogToCombatOrGameConsole($"You counter {en.Name} for {counterDmg} damage!"); yield return StartCoroutine(WaitWhileTypingAndDelay(0f));
                en.TakeDamage(counterDmg); enemyStatusUI?.UpdateDisplay();  player.ConsumeCounterattackBuff(); // ConsumeCounterattackBuff logs its own message
                yield return StartCoroutine(WaitWhileTypingAndDelay(0f));
                if (en.IsDefeated()) LogToCombatOrGameConsole($"{en.Name} was defeated by your counter!"); else LogToCombatOrGameConsole($"{en.Name} HP: {en.CurrentHealth}/{en.MaxHealth}");
                yield return StartCoroutine(WaitWhileTypingAndDelay(0f));
            }
            else
            {
                player.TakeDamage(enemyDamage); // Player.TakeDamage will log rage gain and its own damage via LogPlayerMessage
                yield return StartCoroutine(WaitWhileTypingAndDelay(0f)); // Wait for player's TakeDamage messages
                LogToCombatOrGameConsole($"Your HP: {player.CurrentHealth}/{player.MaxHealth}"); // Then current HP
                yield return StartCoroutine(WaitWhileTypingAndDelay(0f));
            }
            if (player.CurrentHealth <= 0) { if (!player.IsCounterattackActive) LogToCombatOrGameConsole("You have been defeated!"); yield return StartCoroutine(WaitWhileTypingAndDelay(0f)); pStillAlive = false; break; }
            if (en.IsDefeated()) { CheckCombatEnd(); if (CurrentState != GameState.Combat) { pStillAlive = false; break; } }
            yield return new WaitForSeconds(delayBetweenEnemyAttacks);
        }
        CheckCombatEnd();
        if (CurrentState == GameState.Combat && pStillAlive)
        {
            LogToCombatOrGameConsole("--- Your Turn ---"); yield return StartCoroutine(WaitWhileTypingAndDelay(0.1f));
            combatMenuUI?.TransitionToState(CombatMenuState.Main); combatMenuUI?.EnableInput();
        }
    }

    private IEnumerator AttemptFleeSequence()
    {
        LogToCombatOrGameConsole("You attempt to flee..."); yield return StartCoroutine(WaitWhileTypingAndDelay(delayBetweenActions * 0.5f));
        if (Random.Range(0f, 1f) <= 0.75f) { LogToCombatOrGameConsole("...successfully escaped!"); yield return StartCoroutine(WaitWhileTypingAndDelay(delayAfterMajorMessage)); EndCombat(false); }
        else
        {
            LogToCombatOrGameConsole("...but fail to escape!"); yield return StartCoroutine(WaitWhileTypingAndDelay(delayBetweenActions));
            if (CurrentState == GameState.Combat) { if (combatSequenceCoroutine != null) StopCoroutine(combatSequenceCoroutine); combatSequenceCoroutine = StartCoroutine(EnemyTurnSequence()); }
        }
    }

    private IEnumerator SearchLocationSequence(Location locationToSearch)
    {
        // Wait for "searching" message to potentially type out if console has typewriter
        // This uses the game console, not combat logger, so direct delay.
        yield return new WaitForSeconds(delayAfterMajorMessage); // Use a general delay

        bool foundEnemies = PerformEncounterCheck(locationToSearch, false); // false for "isAmbush"

        if (!foundEnemies && CurrentState == GameState.Playing) // Ensure still in Playing state if nothing found
        {
            // Only log "nothing found" if PerformEncounterCheck didn't start combat
            LogToCombatOrGameConsole("You search the area but find nothing of immediate threat.");
            yield return StartCoroutine(WaitWhileTypingAndDelay(0f)); // Wait for this message
        }
        // If enemies were found, PerformEncounterCheck would have called StartCombat,
        // and the state would now be Combat. CombatStartSequence takes over messages.
    }
}