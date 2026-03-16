using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Runtime.InteropServices;
using System.Collections.Generic;

/// <summary>
/// Visual padlock with rotating digit wheels.
/// Reads configuration from JavaScript (positions, puzzleId, solution).
/// </summary>
public class SimplePadlock : MonoBehaviour
{
    [Header("Configuration (overridden by JS config in WebGL)")]
    public int codeLength = 4;
    public int puzzleId = 1;
    public string testSolution = "1234";
    public string unlockedText = "UNLOCKED!";

    [Header("UI References")]
    public Transform wheelsContainer;
    public GameObject wheelPrefab;
    public Button unlockButton;
    public TextMeshProUGUI feedbackText;
    public GameObject successPanel;
    public TextMeshProUGUI successText;
    public Transform shackle;

    [Header("Runtime References (auto-populated)")]
    public TextMeshProUGUI[] digitDisplays;
    public Button[] upButtons;
    public Button[] downButtons;
    public DraggableWheel[] draggableWheels;

    [Header("Interaction Mode")]
    public bool useDragInteraction = true;  // If true, use drag; if false, use buttons

    [Header("Audio")]
    public AudioSource audioSource;

    private int[] currentDigits;
    private bool isLocked = true;
    private bool isSubmitting = false;
    private Vector2 shackleClosedPos;
    private Vector2 shackleOpenPos;

    #if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern int GetConfigPositions();

    [DllImport("__Internal")]
    private static extern int GetConfigPuzzleId();

    [DllImport("__Internal")]
    private static extern string GetConfigSolution();

    [DllImport("__Internal")]
    private static extern string GetConfigUnlockedText();

    [DllImport("__Internal")]
    private static extern void EscappSubmitPuzzle(int puzzleId, string solution, string gameObjectName, string callbackMethod);
    #endif

    void Start()
    {
        // Get config from JavaScript in WebGL
        #if UNITY_WEBGL && !UNITY_EDITOR
        codeLength = GetConfigPositions();
        puzzleId = GetConfigPuzzleId();
        testSolution = GetConfigSolution();
        unlockedText = GetConfigUnlockedText();
        Debug.Log($"Config from JS - positions: {codeLength}, puzzleId: {puzzleId}, solution: {testSolution}, unlockedText: {unlockedText}");
        #endif

        // Clamp code length
        codeLength = Mathf.Clamp(codeLength, 1, 10);

        // Initialize digits array
        currentDigits = new int[codeLength];

        // Store shackle positions for animation
        if (shackle != null)
        {
            shackleClosedPos = shackle.GetComponent<RectTransform>().anchoredPosition;
            shackleOpenPos = shackleClosedPos + new Vector2(0, 60);
        }

        // Create wheels dynamically if we have a prefab and container
        if (wheelPrefab != null && wheelsContainer != null)
        {
            CreateWheels();
        }

        SetupButtons();
        UpdateAllDisplays();

        if (successPanel != null)
            successPanel.SetActive(false);

        SetFeedback("Turn the wheels to set the code");
    }

    void CreateWheels()
    {
        // Clear existing wheels
        foreach (Transform child in wheelsContainer)
        {
            Destroy(child.gameObject);
        }

        // Create new arrays
        List<TextMeshProUGUI> displays = new List<TextMeshProUGUI>();
        List<Button> ups = new List<Button>();
        List<Button> downs = new List<Button>();
        List<DraggableWheel> dragWheels = new List<DraggableWheel>();

        // Create wheels
        for (int i = 0; i < codeLength; i++)
        {
            GameObject wheel = Instantiate(wheelPrefab, wheelsContainer);
            wheel.name = "Wheel_" + i;

            // Find components - check both direct child and inside DigitArea
            var digitArea = wheel.transform.Find("DigitArea");
            var display = wheel.transform.Find("DigitDisplay")?.GetComponent<TextMeshProUGUI>();
            if (display == null && digitArea != null)
                display = digitArea.Find("DigitDisplay")?.GetComponent<TextMeshProUGUI>();

            var upBtn = wheel.transform.Find("UpButton")?.GetComponent<Button>();
            var downBtn = wheel.transform.Find("DownButton")?.GetComponent<Button>();

            if (display != null) displays.Add(display);
            if (upBtn != null) ups.Add(upBtn);
            if (downBtn != null) downs.Add(downBtn);

            // Setup draggable wheel
            if (useDragInteraction)
            {
                // Add DraggableWheel component to the digit area
                Transform dragTarget = digitArea ?? wheel.transform;

                var dragWheel = dragTarget.gameObject.GetComponent<DraggableWheel>();
                if (dragWheel == null)
                    dragWheel = dragTarget.gameObject.AddComponent<DraggableWheel>();

                dragWheel.digitDisplay = display;

                int index = i; // Capture for closure
                dragWheel.OnDigitChanged += (digit) => OnWheelDragged(index, digit);

                dragWheels.Add(dragWheel);

                // Hide up/down buttons when using drag
                if (upBtn != null) upBtn.gameObject.SetActive(false);
                if (downBtn != null) downBtn.gameObject.SetActive(false);
            }
        }

        digitDisplays = displays.ToArray();
        upButtons = ups.ToArray();
        downButtons = downs.ToArray();
        draggableWheels = dragWheels.ToArray();
    }

    void OnWheelDragged(int index, int newDigit)
    {
        Debug.Log($"OnWheelDragged: index={index}, newDigit={newDigit}, isLocked={isLocked}, isSubmitting={isSubmitting}");

        if (!isLocked || isSubmitting)
        {
            Debug.Log("Ignoring wheel drag - padlock unlocked or submitting");
            return;
        }

        currentDigits[index] = newDigit;
        Debug.Log($"Updated currentDigits[{index}] = {newDigit}, code is now: {GetCurrentCode()}");
        PlayClick();
    }

    void SetupButtons()
    {
        // Setup up buttons
        for (int i = 0; i < upButtons.Length && i < codeLength; i++)
        {
            int index = i;
            if (upButtons[i] != null)
                upButtons[i].onClick.AddListener(() => IncrementDigit(index));
        }

        // Setup down buttons
        for (int i = 0; i < downButtons.Length && i < codeLength; i++)
        {
            int index = i;
            if (downButtons[i] != null)
                downButtons[i].onClick.AddListener(() => DecrementDigit(index));
        }

        if (unlockButton != null)
            unlockButton.onClick.AddListener(OnUnlockPressed);
    }

    void IncrementDigit(int index)
    {
        if (!isLocked || isSubmitting) return;

        currentDigits[index] = (currentDigits[index] + 1) % 10;
        UpdateDisplay(index);
        PlayClick();
        AnimateWheel(index, true);
    }

    void DecrementDigit(int index)
    {
        if (!isLocked || isSubmitting) return;

        currentDigits[index] = (currentDigits[index] + 9) % 10;
        UpdateDisplay(index);
        PlayClick();
        AnimateWheel(index, false);
    }

    void AnimateWheel(int index, bool up)
    {
        if (index >= digitDisplays.Length || digitDisplays[index] == null) return;
        StartCoroutine(PunchScale(digitDisplays[index].rectTransform));
    }

    System.Collections.IEnumerator PunchScale(RectTransform rect)
    {
        Vector3 original = rect.localScale;
        float elapsed = 0f;
        float duration = 0.1f;

        while (elapsed < duration)
        {
            float scale = 1f + Mathf.Sin(elapsed / duration * Mathf.PI) * 0.15f;
            rect.localScale = original * scale;
            elapsed += Time.deltaTime;
            yield return null;
        }

        rect.localScale = original;
    }

    void UpdateDisplay(int index)
    {
        if (index < digitDisplays.Length && digitDisplays[index] != null)
        {
            digitDisplays[index].text = currentDigits[index].ToString();
        }
    }

    void UpdateAllDisplays()
    {
        for (int i = 0; i < codeLength; i++)
        {
            UpdateDisplay(i);

            // Sync draggable wheel digit
            if (draggableWheels != null && i < draggableWheels.Length && draggableWheels[i] != null)
            {
                draggableWheels[i].SetDigit(currentDigits[i]);
            }
        }
    }

    string GetCurrentCode()
    {
        string code = "";
        for (int i = 0; i < codeLength; i++)
        {
            code += currentDigits[i].ToString();
        }
        return code;
    }

    void OnUnlockPressed()
    {
        if (!isLocked || isSubmitting) return;

        isSubmitting = true;
        SetFeedback("Checking...");

        #if UNITY_WEBGL && !UNITY_EDITOR
        EscappSubmitPuzzle(puzzleId, GetCurrentCode(), gameObject.name, "OnEscappResponse");
        #else
        Invoke(nameof(CheckSolutionOffline), 0.5f);
        #endif
    }

    void CheckSolutionOffline()
    {
        bool correct = GetCurrentCode() == testSolution;
        string response = correct ?
            "{\"success\":true,\"message\":\"Correct!\"}" :
            "{\"success\":false,\"message\":\"Wrong combination!\"}";
        OnEscappResponse(response);
    }

    public void OnEscappResponse(string jsonResponse)
    {
        isSubmitting = false;

        try
        {
            var response = JsonUtility.FromJson<EscappResponse>(jsonResponse);

            if (response.success)
            {
                OnSuccess();
            }
            else
            {
                OnFailed(response.message);
            }
        }
        catch
        {
            OnFailed("Error checking code");
        }
    }

    void OnSuccess()
    {
        isLocked = false;
        SetFeedback(unlockedText);

        // Disable buttons
        foreach (var btn in upButtons)
            if (btn != null) btn.interactable = false;
        foreach (var btn in downButtons)
            if (btn != null) btn.interactable = false;
        if (unlockButton != null)
            unlockButton.interactable = false;

        // Disable draggable wheels
        if (draggableWheels != null)
        {
            foreach (var wheel in draggableWheels)
                if (wheel != null) wheel.enabled = false;
        }

        // Animate shackle opening
        StartCoroutine(OpenShackle());
    }

    System.Collections.IEnumerator OpenShackle()
    {
        if (shackle == null) yield break;

        RectTransform rect = shackle.GetComponent<RectTransform>();
        float elapsed = 0f;
        float duration = 0.5f;

        while (elapsed < duration)
        {
            rect.anchoredPosition = Vector2.Lerp(shackleClosedPos, shackleOpenPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        rect.anchoredPosition = shackleOpenPos;

        elapsed = 0f;
        Quaternion startRot = rect.localRotation;
        Quaternion endRot = Quaternion.Euler(0, 0, 30);

        while (elapsed < duration)
        {
            rect.localRotation = Quaternion.Lerp(startRot, endRot, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        rect.localRotation = endRot;

        yield return new WaitForSeconds(0.3f);
        if (successPanel != null)
        {
            // Update the success text if available
            if (successText != null)
                successText.text = unlockedText;
            successPanel.SetActive(true);
        }
    }

    void OnFailed(string message)
    {
        SetFeedback(message ?? "Wrong combination!");
        StartCoroutine(ShakePadlock());
    }

    System.Collections.IEnumerator ShakePadlock()
    {
        RectTransform rect = GetComponent<RectTransform>();
        if (rect == null) yield break;

        Vector2 original = rect.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < 0.4f)
        {
            float x = Mathf.Sin(elapsed * 60f) * 12f * (1f - elapsed / 0.4f);
            rect.anchoredPosition = original + new Vector2(x, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }

        rect.anchoredPosition = original;
    }

    void SetFeedback(string message)
    {
        if (feedbackText != null)
            feedbackText.text = message;
    }

    void PlayClick()
    {
        if (audioSource != null)
            audioSource.Play();
    }
}

[System.Serializable]
public class EscappResponse
{
    public bool success;
    public string message;
}
