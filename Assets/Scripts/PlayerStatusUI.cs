// File: PlayerStatusUI.cs
using UnityEngine;
using UnityEngine.UI; // Required for Image
using TMPro;          // Required for TextMeshProUGUI

public class PlayerStatusUI : MonoBehaviour
{
    [Header("Player Reference")]
    public Player player; // Assign in Inspector

    [Header("Health Bar UI")]
    public Image healthBarFill;
    public TextMeshProUGUI healthValueText; // Optional

    [Header("Resource Bar UI")]
    public Image resourceBarFill;
    public TextMeshProUGUI resourceValueText; // Optional
    public TextMeshProUGUI resourceTypeText;  // Optional

    [Header("Experience Bar UI")] // *** NEW UI REFERENCES ***
    public Image experienceBarFill;
    public TextMeshProUGUI experienceValueText; // Optional: For "CurrentXP / NextLevelXP"
    public TextMeshProUGUI levelText;           // Optional: For "Level: X"

    [Header("Resource Bar Colors")]
    public Color manaColor = new Color(0.2f, 0.4f, 1f, 1f);
    public Color rageColor = new Color(0.8f, 0.1f, 0.1f, 1f);
    public Color energyColor = new Color(1f, 0.8f, 0.2f, 1f);
    public Color faithColor = new Color(1f, 0.9f, 0.5f, 1f);
    public Color defaultResourceColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    public Color experienceBarColor = new Color(0.3f, 0.8f, 0.3f, 1f); // Green for XP

    private bool isUiElementsAssigned = false;
    private bool isPlayerReadyForUi = false;

    void Awake()
    {
        // Validate essential UI component references early
        bool coreBarsAssigned = true;
        if (healthBarFill == null) { Debug.LogError("PlayerStatusUI: Health Bar Fill Image not assigned!", this); coreBarsAssigned = false; }
        if (resourceBarFill == null) { Debug.LogError("PlayerStatusUI: Resource Bar Fill Image not assigned!", this); coreBarsAssigned = false; }
        if (experienceBarFill == null) { Debug.LogError("PlayerStatusUI: Experience Bar Fill Image not assigned!", this); coreBarsAssigned = false; } // *** NEW CHECK ***

        isUiElementsAssigned = coreBarsAssigned;

        // Optional text warnings
        if (healthValueText == null) Debug.LogWarning("PlayerStatusUI: Health Value Text not assigned.", this);
        if (resourceValueText == null) Debug.LogWarning("PlayerStatusUI: Resource Value Text not assigned.", this);
        if (resourceTypeText == null) Debug.LogWarning("PlayerStatusUI: Resource Type Text not assigned.", this);
        if (experienceValueText == null) Debug.LogWarning("PlayerStatusUI: Experience Value Text not assigned.", this); // *** NEW CHECK ***
        if (levelText == null) Debug.LogWarning("PlayerStatusUI: Level Text not assigned.", this);                 // *** NEW CHECK ***
    }

    void Start()
    {
        if (!isUiElementsAssigned)
        {
            Debug.LogError("PlayerStatusUI: Essential bar fill images are not assigned. Disabling script.", this);
            gameObject.SetActive(false);
            return;
        }

        if (player == null) player = FindFirstObjectByType<Player>();

        if (player == null)
        {
            Debug.LogWarning("PlayerStatusUI: Player reference not set/found. UI inactive until player available.", this);
            SetBarsToDefaultEmpty();
        }
        else
        {
            TryInitializeAndRefresh();
        }
        // Set XP bar fill color if assigned
        if (experienceBarFill != null) experienceBarFill.color = experienceBarColor;
    }

    void Update()
    {
        if (!isUiElementsAssigned) return;

        if (player == null || !player.IsInitialized)
        {
            if (isPlayerReadyForUi) { SetBarsToDefaultEmpty(); isPlayerReadyForUi = false; }
            return;
        }

        if (!isPlayerReadyForUi) TryInitializeAndRefresh();

        if (isPlayerReadyForUi)
        {
            UpdateHealthBar();
            UpdateResourceBar();
            UpdateExperienceBarAndLevel(); // *** NEW CALL ***
        }
    }

    public void TryInitializeAndRefresh()
    {
        if (!isUiElementsAssigned) return;

        if (player == null || !player.IsInitialized)
        {
            isPlayerReadyForUi = false;
            SetBarsToDefaultEmpty();
            return;
        }

        UpdateResourceBarAppearance();
        UpdateHealthBar();
        UpdateResourceBar();
        UpdateExperienceBarAndLevel(); // *** NEW CALL ***
        isPlayerReadyForUi = true;
        // Debug.Log("PlayerStatusUI: UI Initialized/Refreshed for " + player.PlayerName, this);
    }

    private void SetBarsToDefaultEmpty()
    {
        if (healthBarFill != null) healthBarFill.fillAmount = 0;
        if (healthValueText != null) healthValueText.text = "--- / ---";

        if (resourceBarFill != null)
        {
            resourceBarFill.fillAmount = 0;
            resourceBarFill.color = defaultResourceColor;
            resourceBarFill.gameObject.SetActive(true);
        }
        if (resourceValueText != null) { resourceValueText.text = "--- / ---"; resourceValueText.gameObject.SetActive(true); }
        if (resourceTypeText != null) { resourceTypeText.text = "Resource:"; resourceTypeText.gameObject.SetActive(true); }

        // *** NEW: Set XP Bar and Level Text to default/empty ***
        if (experienceBarFill != null) experienceBarFill.fillAmount = 0;
        if (experienceValueText != null) experienceValueText.text = "XP: --- / ---";
        if (levelText != null) levelText.text = "Level: --";
    }

    void UpdateHealthBar()
    {
        if (player == null || healthBarFill == null) return;
        healthBarFill.fillAmount = (player.MaxHealth > 0) ? (float)player.CurrentHealth / player.MaxHealth : 0;
        if (healthValueText != null) healthValueText.text = $"{player.CurrentHealth} / {player.MaxHealth}";
    }

    void UpdateResourceBarAppearance()
    {
        if (player == null || resourceBarFill == null) return;
        // ... (Resource bar appearance logic - no changes here) ...
        string resourceName = "Resource"; Color targetColor = defaultResourceColor; bool barShouldBeActive = true;
        switch (player.Class)
        {
            case PlayerClass.Wizard: case PlayerClass.Ranger: targetColor = manaColor; resourceName = "Mana"; break;
            case PlayerClass.Cleric: targetColor = faithColor; resourceName = "Mana"; break;
            case PlayerClass.Fighter: targetColor = rageColor; resourceName = "Rage"; break;
            case PlayerClass.Scout: targetColor = energyColor; resourceName = "Energy"; break;
            default: resourceName = "N/A"; barShouldBeActive = false; break;
        }
        resourceBarFill.color = targetColor; resourceBarFill.gameObject.SetActive(barShouldBeActive);
        if (resourceTypeText != null) { resourceTypeText.gameObject.SetActive(barShouldBeActive); resourceTypeText.text = barShouldBeActive ? (resourceName + ":") : ""; }
        if (resourceValueText != null) resourceValueText.gameObject.SetActive(barShouldBeActive);
    }

    void UpdateResourceBar()
    {
        if (player == null || resourceBarFill == null || !resourceBarFill.gameObject.activeInHierarchy) return;
        // ... (Resource bar update logic - no changes here) ...
        float currentResource = 0; float maxResource = 0;
        switch (player.Class)
        {
            case PlayerClass.Wizard: case PlayerClass.Ranger: case PlayerClass.Cleric: currentResource = player.CurrentMana; maxResource = player.MaxMana; break;
            case PlayerClass.Fighter: currentResource = player.CurrentRage; maxResource = player.MaxRage; break;
            case PlayerClass.Scout: currentResource = player.CurrentEnergy; maxResource = player.MaxEnergy; break;
            default: resourceBarFill.fillAmount = 0; if (resourceValueText != null) resourceValueText.text = "0 / 0"; return;
        }
        resourceBarFill.fillAmount = (maxResource > 0) ? currentResource / maxResource : 0;
        if (resourceValueText != null) resourceValueText.text = $"{(int)currentResource} / {(int)maxResource}";
    }

    // *** NEW METHOD to update XP Bar and Level Text ***
    void UpdateExperienceBarAndLevel()
    {
        if (player == null) return; // Player check

        // Update XP Bar
        if (experienceBarFill != null)
        {
            if (player.ExperienceToNextLevel > 0)
            {
                experienceBarFill.fillAmount = (float)player.CurrentExperience / player.ExperienceToNextLevel;
            }
            else // Should not happen if level progression is set up, but handle division by zero
            {
                experienceBarFill.fillAmount = (player.Level > 0) ? 1 : 0; // Max level or error state
            }
        }

        // Update XP Text
        if (experienceValueText != null)
        {
            experienceValueText.text = $"XP: {player.CurrentExperience} / {player.ExperienceToNextLevel}";
        }

        // Update Level Text
        if (levelText != null)
        {
            levelText.text = $"Level: {player.Level}";
        }
    }
}