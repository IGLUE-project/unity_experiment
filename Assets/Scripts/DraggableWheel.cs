using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// Draggable digit wheel with visual scrolling effect.
/// Drag up/down to change the digit - numbers scroll visually.
/// </summary>
public class DraggableWheel : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("References")]
    public TextMeshProUGUI digitDisplay;
    public RectTransform scrollContainer;

    [Header("Settings")]
    public float dragThreshold = 50f;  // Pixels to drag for one digit change
    public float snapSpeed = 10f;      // Speed of snap animation

    // Events
    public event Action<int> OnDigitChanged;

    // Internal state
    private int currentDigit = 0;
    private bool isDragging = false;
    private Vector2 lastPointerPos;
    private float visualOffset = 0f;      // Current visual offset in pixels
    private float digitHeight = 60f;      // Height of one digit slot

    // For showing adjacent digits
    private TextMeshProUGUI prevDigitText;
    private TextMeshProUGUI nextDigitText;
    private RectTransform digitContainer;

    void Start()
    {
        SetupScrollingDigits();
    }

    void SetupScrollingDigits()
    {
        if (digitDisplay == null) return;

        // Get the digit height from the display
        digitHeight = digitDisplay.rectTransform.rect.height;
        if (digitHeight <= 0) digitHeight = 60f;

        // Create a container for the scrolling digits
        digitContainer = new GameObject("DigitContainer").AddComponent<RectTransform>();
        digitContainer.SetParent(digitDisplay.transform.parent, false);
        digitContainer.anchorMin = Vector2.zero;
        digitContainer.anchorMax = Vector2.one;
        digitContainer.sizeDelta = Vector2.zero;
        digitContainer.anchoredPosition = Vector2.zero;

        // Move the main digit display into the container
        digitDisplay.transform.SetParent(digitContainer, false);
        digitDisplay.rectTransform.anchorMin = new Vector2(0, 0);
        digitDisplay.rectTransform.anchorMax = new Vector2(1, 1);
        digitDisplay.rectTransform.sizeDelta = Vector2.zero;
        digitDisplay.rectTransform.anchoredPosition = Vector2.zero;

        // Create previous digit (above) - starts hidden
        GameObject prevObj = new GameObject("PrevDigit");
        prevObj.transform.SetParent(digitContainer, false);
        prevDigitText = prevObj.AddComponent<TextMeshProUGUI>();
        CopyTextStyle(digitDisplay, prevDigitText, true);
        prevDigitText.rectTransform.anchorMin = new Vector2(0, 1);
        prevDigitText.rectTransform.anchorMax = new Vector2(1, 1);
        prevDigitText.rectTransform.pivot = new Vector2(0.5f, 0);
        prevDigitText.rectTransform.sizeDelta = new Vector2(0, digitHeight);
        prevDigitText.rectTransform.anchoredPosition = Vector2.zero;

        // Create next digit (below) - starts hidden
        GameObject nextObj = new GameObject("NextDigit");
        nextObj.transform.SetParent(digitContainer, false);
        nextDigitText = nextObj.AddComponent<TextMeshProUGUI>();
        CopyTextStyle(digitDisplay, nextDigitText, true);
        nextDigitText.rectTransform.anchorMin = new Vector2(0, 0);
        nextDigitText.rectTransform.anchorMax = new Vector2(1, 0);
        nextDigitText.rectTransform.pivot = new Vector2(0.5f, 1);
        nextDigitText.rectTransform.sizeDelta = new Vector2(0, digitHeight);
        nextDigitText.rectTransform.anchoredPosition = Vector2.zero;

        UpdateAllDigitDisplays();
    }

    void CopyTextStyle(TextMeshProUGUI source, TextMeshProUGUI target, bool startHidden = false)
    {
        target.font = source.font;
        target.fontSize = source.fontSize;
        target.fontStyle = source.fontStyle;
        target.color = startHidden ? new Color(source.color.r, source.color.g, source.color.b, 0f) : source.color;
        target.alignment = source.alignment;
        target.raycastTarget = false;
    }

    public void SetDigit(int digit)
    {
        currentDigit = ((digit % 10) + 10) % 10;
        visualOffset = 0f;
        UpdateAllDigitDisplays();
        UpdateVisualPosition();
    }

    public int GetDigit()
    {
        return currentDigit;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        lastPointerPos = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        float deltaY = eventData.position.y - lastPointerPos.y;
        lastPointerPos = eventData.position;

        // Update visual offset
        visualOffset += deltaY;

        // Check if we've scrolled enough for a digit change
        while (visualOffset >= dragThreshold)
        {
            visualOffset -= dragThreshold;
            IncrementDigit();
        }

        while (visualOffset <= -dragThreshold)
        {
            visualOffset += dragThreshold;
            DecrementDigit();
        }

        UpdateVisualPosition();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        // Snap back to center
        visualOffset = 0f;
        UpdateVisualPosition();
    }

    void Update()
    {
        // Smooth snap animation when not dragging
        if (!isDragging && Mathf.Abs(visualOffset) > 0.1f)
        {
            visualOffset = Mathf.Lerp(visualOffset, 0f, Time.deltaTime * snapSpeed);
            UpdateVisualPosition();
        }
    }

    void IncrementDigit()
    {
        currentDigit = (currentDigit + 1) % 10;
        UpdateAllDigitDisplays();
        OnDigitChanged?.Invoke(currentDigit);
    }

    void DecrementDigit()
    {
        currentDigit = (currentDigit - 1 + 10) % 10;
        UpdateAllDigitDisplays();
        OnDigitChanged?.Invoke(currentDigit);
    }

    void UpdateAllDigitDisplays()
    {
        if (digitDisplay != null)
        {
            digitDisplay.text = currentDigit.ToString();
        }

        if (prevDigitText != null)
        {
            int prevDigit = (currentDigit + 1) % 10;  // Drag up = increment
            prevDigitText.text = prevDigit.ToString();
        }

        if (nextDigitText != null)
        {
            int nextDigit = (currentDigit - 1 + 10) % 10;  // Drag down = decrement
            nextDigitText.text = nextDigit.ToString();
        }
    }

    void UpdateVisualPosition()
    {
        if (digitContainer == null) return;

        // Move the container based on visual offset
        // Clamp the visual movement to reasonable bounds
        float clampedOffset = Mathf.Clamp(visualOffset, -dragThreshold, dragThreshold);
        digitContainer.anchoredPosition = new Vector2(0, clampedOffset);

        // Fade adjacent digits based on how close they are to being visible
        float fadeAmount = Mathf.Abs(clampedOffset) / dragThreshold;

        if (prevDigitText != null)
        {
            Color c = prevDigitText.color;
            c.a = clampedOffset > 0 ? fadeAmount * 0.6f : 0f;
            prevDigitText.color = c;
        }

        if (nextDigitText != null)
        {
            Color c = nextDigitText.color;
            c.a = clampedOffset < 0 ? fadeAmount * 0.6f : 0f;
            nextDigitText.color = c;
        }
    }
}
