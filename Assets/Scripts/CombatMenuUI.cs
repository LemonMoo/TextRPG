// File: CombatMenuUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class CombatMenuUI : MonoBehaviour
{
    [Header("Main Action Panel UI")]
    public GameObject actionSelectionPanel;
    public TextMeshProUGUI attackText;
    public TextMeshProUGUI skillCastText;
    public TextMeshProUGUI itemText;
    public TextMeshProUGUI fleeText;

    [Header("Skill Selection Panel UI")]
    public GameObject skillSelectionPanel;
    public List<TextMeshProUGUI> skillOptionTextsList;
    public TextMeshProUGUI skillBackText;

    [Header("Selection Visuals")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;
    public Color disabledColor = new Color(0.6f, 0.6f, 0.6f, 1f);

    private CombatMenuState currentMenuState = CombatMenuState.Main;
    private bool canProcessInput = false;

    private TextMeshProUGUI[] mainOptionTextsArray;
    private int mainCurrentIndex = 0;
    private CombatActionType[] mainActionOrder = { CombatActionType.ATTACK, CombatActionType.CAST, CombatActionType.ITEM, CombatActionType.FLEE };

    private List<FighterSkillType> currentlyDisplayedPlayerSkills = new List<FighterSkillType>();
    private TextMeshProUGUI[] currentSkillMenuNavigableItemsArray;
    private int skillCurrentIndex = 0;

    void Awake()
    {
        mainOptionTextsArray = new[] { attackText, skillCastText, itemText, fleeText };
        if (actionSelectionPanel == null || skillSelectionPanel == null || mainOptionTextsArray.Any(t => t == null) || skillOptionTextsList == null || skillBackText == null)
        {
            Debug.LogError("CombatMenuUI: Critical UI references missing!", this); enabled = false; return;
        }
        skillSelectionPanel.SetActive(false); actionSelectionPanel.SetActive(true);
    }

    void OnEnable()
    {
        TransitionToState(CombatMenuState.Main);
        // GameManager will call EnableInput() when it's time for player to act.
    }

    void OnDisable()
    {
        DisableInput();
    }

    void Update()
    {
        if (!canProcessInput || GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Combat) return;
        switch (currentMenuState)
        {
            case CombatMenuState.Main: HandleNavigationInput(mainOptionTextsArray, ref mainCurrentIndex, true); HandleConfirmInput_Main(); break;
            case CombatMenuState.Skills: HandleNavigationInput(currentSkillMenuNavigableItemsArray, ref skillCurrentIndex, false); HandleConfirmInput_Skills(); break;
        }
    }

    public void EnableInput()
    {
        canProcessInput = true;
        UpdateAllOptionVisuals();
        Debug.Log($"CombatMenuUI: Input ENABLED for state: {currentMenuState}", this);
    }

    public void DisableInput()
    {
        canProcessInput = false;
        // UpdateAllOptionVisuals(); // To show options as "dimmed" perhaps
        Debug.Log($"CombatMenuUI: Input DISABLED for state: {currentMenuState}", this);
    }

    public void TransitionToState(CombatMenuState newState)
    {
        CombatMenuState oldState = currentMenuState; // Store old state
        currentMenuState = newState;
        Debug.Log($"CombatMenuUI: Transitioning from {oldState} to menu state: {currentMenuState}", this);

        actionSelectionPanel.SetActive(currentMenuState == CombatMenuState.Main);
        skillSelectionPanel.SetActive(currentMenuState == CombatMenuState.Skills);

        if (currentMenuState == CombatMenuState.Main)
        {
            mainCurrentIndex = 0;
        }
        else if (currentMenuState == CombatMenuState.Skills)
        {
            PopulateSkillMenuForCurrentPlayer();
            skillCurrentIndex = 0;
        }

        // Always update visuals for the new state.
        // If 'canProcessInput' is true, the highlight will appear.
        // If 'canProcessInput' is false, visuals update but highlight won't show (or will be dimmed).
        UpdateAllOptionVisuals();

        // If we are transitioning INTERNALLY (e.g. main to skill, skill back to main)
        // AND the input was generally allowed (canProcessInput is true),
        // we want the new menu to be responsive immediately.
        // GameManager is responsible for the overall turn's input enabling/disabling.
        // This ensures that if player was allowed to make a choice, and they open a sub-menu,
        // that sub-menu is also ready for input.
        if (canProcessInput)
        { // If input was already allowed before transition
          // No need to call EnableInput() again if canProcessInput is already true,
          // as UpdateAllOptionVisuals() handles setting the correct highlight.
        }
    }   

    void HandleNavigationInput(TextMeshProUGUI[] activeOptions, ref int currentIndex, bool isGridNavigation)
    {
        if (activeOptions == null || activeOptions.Length == 0) return;
        int previousIndex = currentIndex;
        if (isGridNavigation)
        { /* ... 2x2 grid logic ... */
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) { if (currentIndex >= 2) currentIndex -= 2; }
            else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) { if (currentIndex < 2) currentIndex += 2; }
            else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) { if (currentIndex == 1 || currentIndex == 3) currentIndex -= 1; }
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) { if (currentIndex == 0 || currentIndex == 2) currentIndex += 1; }
        }
        else
        { /* ... linear up/down logic ... */
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) { currentIndex--; }
            else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) { currentIndex++; }
        }
        currentIndex = Mathf.Clamp(currentIndex, 0, activeOptions.Length - 1);
        if (previousIndex != currentIndex) UpdateAllOptionVisuals(); // Full refresh on navigation
    }

    void UpdateAllOptionVisuals()
    {
        UpdateOptionNameAndBaseAvailabilityColors();
        UpdateSelectionHighlight();
    }

    void UpdateOptionNameAndBaseAvailabilityColors()
    {
        Player player = GameManager.Instance?.player;
        if (player == null) { /* ... disable all options ... */ return; }

        // Main Menu
        if (skillCastText != null)
        { /* ... set SKILL/CAST text and disabledColor based on player class and skill availability ... */
            bool mainCanUseSkillCast = false;
            if (player.Class == PlayerClass.Fighter) { skillCastText.text = "SKILL"; if (player.CanActivateCounterattack()) mainCanUseSkillCast = true; /* || player.CanUsePowerStrike() etc. */ }
            else if (player.Class == PlayerClass.Wizard || player.Class == PlayerClass.Ranger || player.Class == PlayerClass.Cleric) { skillCastText.text = "CAST"; mainCanUseSkillCast = player.MaxMana > 0; }
            else { skillCastText.text = "---"; mainCanUseSkillCast = false; }
            skillCastText.color = mainCanUseSkillCast ? normalColor : disabledColor;
            attackText.color = normalColor; itemText.color = normalColor; fleeText.color = normalColor;
        }

        // Skill Menu (if active and populated)
        if (currentMenuState == CombatMenuState.Skills && currentSkillMenuNavigableItemsArray != null)
        {
            for (int i = 0; i < currentlyDisplayedPlayerSkills.Count; i++)
            { // Only up to actual skills
                if (i < skillOptionTextsList.Count && skillOptionTextsList[i] != null)
                {
                    bool canUse = false;
                    if (currentlyDisplayedPlayerSkills[i] == FighterSkillType.Counterattack) canUse = player.CanActivateCounterattack();
                    skillOptionTextsList[i].color = canUse ? normalColor : disabledColor;
                }
            }
            if (skillBackText != null) skillBackText.color = normalColor; // Back button is always normal
        }
    }

    void UpdateSelectionHighlight()
    {
        TextMeshProUGUI[] options = null; int idx = -1;
        if (currentMenuState == CombatMenuState.Main) { options = mainOptionTextsArray; idx = mainCurrentIndex; }
        else if (currentMenuState == CombatMenuState.Skills) { options = currentSkillMenuNavigableItemsArray; idx = skillCurrentIndex; }
        if (options == null) return;

        for (int i = 0; i < options.Length; i++)
        {
            if (options[i] != null && options[i].gameObject.activeSelf)
            {
                bool isSelected = (i == idx);
                bool isActuallyDisabled = (options[i].color == disabledColor);
                bool isBackButton = (options[i] == skillBackText && currentMenuState == CombatMenuState.Skills);

                if (isSelected && canProcessInput)
                { // Only highlight if input is allowed
                    if (!isActuallyDisabled || isBackButton) options[i].color = selectedColor;
                    // else it stays disabledColor (even if selected)
                }
                else if (!isActuallyDisabled)
                { // Not selected, not disabled: normal color
                    options[i].color = normalColor;
                }
                // If it's not selected AND it's disabled, it stays disabledColor (no change needed)
            }
        }
    }

    void HandleConfirmInput_Main()
    {
        if (!(Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))) return;
        if (mainCurrentIndex >= mainOptionTextsArray.Length || mainOptionTextsArray[mainCurrentIndex].color == disabledColor && mainOptionTextsArray[mainCurrentIndex] != skillCastText /*Allow selecting disabled SKILL to open menu*/)
        {
            // Allow selecting skillCastText even if "disabled" because it might just mean no *currently usable* skills, but menu should still open
            if (mainOptionTextsArray[mainCurrentIndex] == skillCastText && (GameManager.Instance?.player.Class == PlayerClass.Fighter /* || other skill/spell classes */))
            {
                // Proceed to open skill/spell menu
            }
            else
            {
                Debug.Log("CombatMenuUI: Main action disabled or invalid index.", this); return;
            }
        }

        CombatActionType selectedAction = mainActionOrder[mainCurrentIndex];
        Debug.Log($"CombatMenuUI: Main Menu selected: {selectedAction}", this);

        if (selectedAction == CombatActionType.CAST)
        { // This is SKILL or CAST button
            Player player = GameManager.Instance?.player;
            if (player != null)
            {
                if (player.Class == PlayerClass.Fighter)
                {
                    TransitionToState(CombatMenuState.Skills); // Opens skill sub-menu, input should remain enabled for it
                    // EnableInput(); // No, Transition handles visuals, GameManager dictates turn
                    return;
                }
                // else if (player.Class == PlayerClass.Wizard) { /* Transition to spell menu */ return; }
            }
        }
        DisableInput(); // Disable this UI's input as action is passed to GameManager
        GameManager.Instance?.ProcessPlayerCombatAction(selectedAction);
    }

    void HandleConfirmInput_Skills()
    {
        if (!(Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))) return;
        if (currentSkillMenuNavigableItemsArray == null || skillCurrentIndex >= currentSkillMenuNavigableItemsArray.Length) return;

        bool isBackSelected = currentSkillMenuNavigableItemsArray[skillCurrentIndex] == skillBackText;
        if (!isBackSelected && currentSkillMenuNavigableItemsArray[skillCurrentIndex].color == disabledColor)
        {
            Debug.Log("CombatMenuUI: Cannot use disabled skill.", this); return;
        }

        if (isBackSelected)
        {
            TransitionToState(CombatMenuState.Main); // Opens main menu, input should remain enabled
            // EnableInput(); // No, Transition handles visuals, GameManager dictates turn
            return;
        }

        if (skillCurrentIndex < currentlyDisplayedPlayerSkills.Count)
        {
            FighterSkillType selectedSkill = currentlyDisplayedPlayerSkills[skillCurrentIndex];
            Debug.Log($"CombatMenuUI: Skill selected: {selectedSkill}", this);
            DisableInput(); // Disable this UI's input as skill is passed to GameManager
            GameManager.Instance?.ProcessPlayerFighterSkill(selectedSkill);
        }
    }

    private void PopulateSkillMenuForCurrentPlayer()
    {
        currentlyDisplayedPlayerSkills.Clear(); Player player = GameManager.Instance?.player;
        if (player == null)
        { /* ... handle null player, show only BACK ... */
            currentSkillMenuNavigableItemsArray = skillBackText != null ? new TextMeshProUGUI[] { skillBackText } : new TextMeshProUGUI[0];
            if (skillBackText != null) skillBackText.gameObject.SetActive(true);
            foreach (var ts in skillOptionTextsList) ts?.gameObject.SetActive(false);
            skillCurrentIndex = 0; return;
        }

        if (player.Class == PlayerClass.Fighter) currentlyDisplayedPlayerSkills.Add(FighterSkillType.Counterattack);
        // else if (player.Class == PlayerClass.Wizard) { /* populate wizard spells */ }

        List<TextMeshProUGUI> activeNavTexts = new List<TextMeshProUGUI>();
        for (int i = 0; i < skillOptionTextsList.Count; i++)
        {
            if (i < currentlyDisplayedPlayerSkills.Count)
            {
                skillOptionTextsList[i].text = FormatSkillName(currentlyDisplayedPlayerSkills[i].ToString());
                skillOptionTextsList[i].gameObject.SetActive(true); activeNavTexts.Add(skillOptionTextsList[i]);
            }
            else
            {
                skillOptionTextsList[i].text = ""; skillOptionTextsList[i].gameObject.SetActive(false);
            }
        }
        if (skillBackText != null) { skillBackText.gameObject.SetActive(true); activeNavTexts.Add(skillBackText); }
        currentSkillMenuNavigableItemsArray = activeNavTexts.ToArray();
        skillCurrentIndex = 0; // Default to first item in the populated list
    }

    private string FormatSkillName(string rawName) { return rawName; }
}       