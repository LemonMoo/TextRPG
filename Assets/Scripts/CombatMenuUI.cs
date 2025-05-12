// File: CombatMenuUI.cs
using UnityEngine;
using UnityEngine.UI; // If using a UI Image for selectionIndicator
using TMPro;          // Required for TextMeshPro elements
using System.Linq;    // Required for LINQ .All() in GameManager later

public class CombatMenuUI : MonoBehaviour
{
    [Header("UI Option References")]
    public TextMeshProUGUI attackText;
    public TextMeshProUGUI castText;
    public TextMeshProUGUI itemText;
    public TextMeshProUGUI fleeText;

    [Header("Selection Visuals")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;
    // public Image selectionIndicator; // Uncomment if using a separate Image indicator

    private TextMeshProUGUI[] optionTexts;
    private int currentIndex = 0; // 0: ATTACK, 1: CAST, 2: ITEM, 3: FLEE

    // Corresponds to CombatActionType enum order
    private CombatActionType[] actionOrder = {
        CombatActionType.ATTACK, CombatActionType.CAST,
        CombatActionType.ITEM, CombatActionType.FLEE
    };

    void Awake()
    {
        // Initialize the array of text options for easy access
        optionTexts = new TextMeshProUGUI[] { attackText, castText, itemText, fleeText };

        if (attackText == null || castText == null || itemText == null || fleeText == null)
        {
            Debug.LogError("CombatMenuUI: Not all TextMeshPro option references are assigned in the Inspector!", this);
            this.enabled = false; // Disable script if critical references are missing
            return;
        }
        // if (selectionIndicator == null) Debug.LogWarning("CombatMenuUI: Selection Indicator Image not assigned.", this);
    }

    void OnEnable()
    {
        // Reset to default selection when the menu becomes active
        currentIndex = 0;
        UpdateSelectionVisuals();
        // Consider adding logic to ensure this UI is focused for input if you have multiple active UIs.
    }

    void Update()
    {
        HandleNavigationInput();
        HandleConfirmInput();
    }

    void HandleNavigationInput()
    {
        int previousIndex = currentIndex;

        // Vertical Navigation (W/S or Up/Down Arrows)
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (currentIndex == 2 || currentIndex == 3) // If on ITEM or FLEE
            {
                currentIndex -= 2; // Move to ATTACK or CAST
            }
        }
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            if (currentIndex == 0 || currentIndex == 1) // If on ATTACK or CAST
            {
                currentIndex += 2; // Move to ITEM or FLEE
            }
        }
        // Horizontal Navigation (A/D or Left/Right Arrows)
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (currentIndex == 1 || currentIndex == 3) // If on CAST or FLEE
            {
                currentIndex -= 1; // Move to ATTACK or ITEM
            }
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (currentIndex == 0 || currentIndex == 2) // If on ATTACK or ITEM
            {
                currentIndex += 1; // Move to CAST or FLEE
            }
        }

        // Ensure index stays within bounds (0 to 3)
        currentIndex = Mathf.Clamp(currentIndex, 0, optionTexts.Length - 1);

        if (previousIndex != currentIndex)
        {
            UpdateSelectionVisuals();
        }
    }

    void HandleConfirmInput()
    {
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) // Space or Enter
        {
            ConfirmSelection();
        }
    }

    void UpdateSelectionVisuals()
    {
        for (int i = 0; i < optionTexts.Length; i++)
        {
            if (optionTexts[i] != null)
            {
                optionTexts[i].color = (i == currentIndex) ? selectedColor : normalColor;
            }
        }

        // If using a separate selection indicator image:
        // if (selectionIndicator != null && optionTexts[currentIndex] != null)
        // {
        //     // This makes the indicator a child of the text's parent, then positions it.
        //     // More robust positioning might involve setting anchors or using a Layout Group.
        //     selectionIndicator.rectTransform.SetParent(optionTexts[currentIndex].transform.parent);
        //     selectionIndicator.rectTransform.position = optionTexts[currentIndex].transform.position;
        //     // You might need to adjust the indicator's size or add an offset to position it nicely.
        // }
    }

    void ConfirmSelection()
    {
        CombatActionType selectedAction = actionOrder[currentIndex];
        Debug.Log($"Player selected: {selectedAction}");

        // Notify the GameManager (or a dedicated CombatManager)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ProcessPlayerCombatAction(selectedAction);
            // Optionally, you might want to disable this menu briefly or wait for the action to resolve
            // For example: this.gameObject.SetActive(false); if GameManager handles re-enabling it.
        }
        else
        {
            Debug.LogError("CombatMenuUI: GameManager.Instance is not found!", this);
        }
    }
}