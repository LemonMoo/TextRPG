// File: CombatLoggerUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class CombatLoggerUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI combatLogText;
    public ScrollRect combatLogScrollRect;

    [Header("Log Settings")]
    public int maxLogLines = 50;
    public float characterTypeDelay = 0.03f;

    private List<string> logMessages = new List<string>();
    private Coroutine currentTypewriterCoroutine;
    private bool isCurrentlyTyping = false;
    private Queue<string> messageQueue = new Queue<string>();

    void Awake()
    {
        if (combatLogText == null) { Debug.LogError("CombatLoggerUI: Combat Log Text not assigned!", this); this.enabled = false; return; }
        ClearLogInstantly();
    }

    void OnEnable()
    {
        ProcessMessageQueue();
    }

    public void AddMessage(string message)
    {
        if (string.IsNullOrEmpty(message)) return;
        string[] lines = message.Split('\n');
        foreach (string line in lines) messageQueue.Enqueue(line);
        ProcessMessageQueue();
    }

    private void ProcessMessageQueue()
    {
        if (!isCurrentlyTyping && messageQueue.Count > 0)
        {
            string nextMessage = messageQueue.Dequeue();
            if (currentTypewriterCoroutine != null) StopCoroutine(currentTypewriterCoroutine);
            currentTypewriterCoroutine = StartCoroutine(TypeLine(nextMessage));
        }
    }

    private IEnumerator TypeLine(string lineToType)
    {
        isCurrentlyTyping = true;
        if (logMessages.Count >= maxLogLines && maxLogLines > 0) logMessages.RemoveAt(0);
        logMessages.Add(""); // Add empty slot for the new line
        UpdateLogTextInstantly(); // Show the new empty line
        ScrollToBottom();

        int currentLineIndex = logMessages.Count - 1;
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < lineToType.Length; i++)
        {
            sb.Append(lineToType[i]);
            logMessages[currentLineIndex] = sb.ToString();
            combatLogText.text = string.Join("\n", logMessages);
            ScrollToBottom(); // Keep scrolling
            yield return new WaitForSeconds(characterTypeDelay);
        }
        isCurrentlyTyping = false;
        ProcessMessageQueue();
    }

    public void ClearLogInstantly()
    {
        if (currentTypewriterCoroutine != null) { StopCoroutine(currentTypewriterCoroutine); isCurrentlyTyping = false; }
        messageQueue.Clear();
        logMessages.Clear();
        UpdateLogTextInstantly();
    }

    private void UpdateLogTextInstantly()
    {
        if (combatLogText != null) combatLogText.text = string.Join("\n", logMessages);
    }

    private void ScrollToBottom()
    {
        if (combatLogScrollRect != null && combatLogScrollRect.gameObject.activeInHierarchy)
        {
            StartCoroutine(ScrollToBottomAfterFrame());
        }
    }

    private IEnumerator ScrollToBottomAfterFrame()
    {
        yield return null; // Wait for end of frame for layout to settle
        if (combatLogScrollRect != null) combatLogScrollRect.normalizedPosition = new Vector2(0, 0);
    }

    // Public method for GameManager to check if text is still typing out
    public bool IsTyping()
    {
        return isCurrentlyTyping; // Simplified: just checks if currently in the TypeLine coroutine
                                  // If GameManager proceeds too fast, we can change to:
                                  // return isCurrentlyTyping || messageQueue.Count > 0;
    }
}