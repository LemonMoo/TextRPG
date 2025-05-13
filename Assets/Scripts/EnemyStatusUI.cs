// File: EnemyStatusUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnemyStatusUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI enemyNameText;
    public TextMeshProUGUI enemyLevelText; // Optional
    public Image enemyHpBarFill;
    public TextMeshProUGUI enemyHpValueText; // Optional
    // Future: public GameObject statusEffectsArea;

    private Enemy currentTargetEnemy;

    void Awake()
    {
        // Validate essential references
        if (enemyNameText == null) Debug.LogError("EnemyStatusUI: Enemy Name Text not assigned!", this);
        if (enemyHpBarFill == null) Debug.LogError("EnemyStatusUI: Enemy HP Bar Fill not assigned!", this);
        // Optional
        if (enemyLevelText == null) Debug.LogWarning("EnemyStatusUI: Enemy Level Text not assigned.", this);
        if (enemyHpValueText == null) Debug.LogWarning("EnemyStatusUI: Enemy HP Value Text not assigned.", this);

        // Hide by default, GameManager will activate panel and call SetTarget
        gameObject.SetActive(false);
    }

    void Update()
    {
        // If we have a target and this UI is active, update its HP bar
        if (currentTargetEnemy != null && gameObject.activeInHierarchy)
        {
            UpdateHpBar();
        }
        else if (currentTargetEnemy == null && gameObject.activeInHierarchy)
        {
            // If UI is active but no target, clear/hide elements
            ClearDisplay();
        }
    }

    // Called by GameManager to set the enemy whose stats should be displayed
    public void SetTargetEnemy(Enemy enemy)
    {
        currentTargetEnemy = enemy;
        if (currentTargetEnemy != null)
        {
            gameObject.SetActive(true); // Show this panel
            UpdateDisplay();
        }
        else
        {
            ClearDisplay();
            gameObject.SetActive(false); // Hide this panel if no target
        }
    }

    public void UpdateDisplay() // Call this if enemy stats change (e.g., after taking damage)
    {
        if (currentTargetEnemy == null)
        {
            ClearDisplay();
            return;
        }

        if (enemyNameText != null) enemyNameText.text = currentTargetEnemy.Name;
        if (enemyLevelText != null)
        {
            // Assuming Enemy.cs has a Level property
            if (System.Array.Exists(currentTargetEnemy.GetType().GetProperties(), p => p.Name == "Level"))
            {
                enemyLevelText.text = $"Lvl: {currentTargetEnemy.Level}"; // Requires Enemy.Level
                enemyLevelText.gameObject.SetActive(true);
            }
            else
            {
                enemyLevelText.gameObject.SetActive(false); // Hide if no level property
            }
        }
        UpdateHpBar();
        // Future: UpdateStatusEffectsDisplay();
    }

    private void UpdateHpBar()
    {
        if (currentTargetEnemy != null && enemyHpBarFill != null)
        {
            enemyHpBarFill.fillAmount = (currentTargetEnemy.MaxHealth > 0) ?
                (float)currentTargetEnemy.CurrentHealth / currentTargetEnemy.MaxHealth : 0;

            if (enemyHpValueText != null)
            {
                enemyHpValueText.text = $"{currentTargetEnemy.CurrentHealth} / {currentTargetEnemy.MaxHealth}";
            }
        }
    }

    public void ClearDisplay()
    {
        if (enemyNameText != null) enemyNameText.text = "---";
        if (enemyLevelText != null) enemyLevelText.text = "Lvl: --";
        if (enemyHpBarFill != null) enemyHpBarFill.fillAmount = 0;
        if (enemyHpValueText != null) enemyHpValueText.text = "--- / ---";
        currentTargetEnemy = null;
    }

    // Call this from GameManager when combat ends
    public void HideDisplay()
    {
        ClearDisplay();
        gameObject.SetActive(false);
    }
}