using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEditor.SceneManagement;
using TMPro;

public class CreatePadlockScene : EditorWindow
{
    [MenuItem("Tools/Padlock Puzzle/Create Padlock Scene")]
    public static void CreateScene()
    {
        // Create new scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Set camera background to transparent for WebGL
        Camera.main.backgroundColor = new Color(0, 0, 0, 0);
        Camera.main.clearFlags = CameraClearFlags.SolidColor;

        // Add TransparentBackground component to ensure transparency at runtime
        Camera.main.gameObject.AddComponent<TransparentBackground>();

        // Create Canvas
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        // Create EventSystem
        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // Colors
        Color metalDark = new Color(0.25f, 0.25f, 0.28f);
        Color metalLight = new Color(0.4f, 0.4f, 0.45f);
        Color gold = new Color(0.85f, 0.65f, 0.2f);
        Color goldDark = new Color(0.6f, 0.45f, 0.15f);

        // Main padlock container
        GameObject padlock = new GameObject("Padlock");
        padlock.transform.SetParent(canvasObj.transform, false);
        RectTransform padlockRect = padlock.AddComponent<RectTransform>();
        padlockRect.anchorMin = new Vector2(0.5f, 0.5f);
        padlockRect.anchorMax = new Vector2(0.5f, 0.5f);
        padlockRect.sizeDelta = new Vector2(320, 450);

        // Add SimplePadlock component
        SimplePadlock padlockScript = padlock.AddComponent<SimplePadlock>();
        padlockScript.codeLength = 4;
        padlockScript.testSolution = "1234";
        padlockScript.audioSource = padlock.AddComponent<AudioSource>();

        // === SHACKLE ===
        GameObject shackle = new GameObject("Shackle");
        shackle.transform.SetParent(padlock.transform, false);
        RectTransform shackleRect = shackle.AddComponent<RectTransform>();
        shackleRect.anchorMin = new Vector2(0.5f, 1f);
        shackleRect.anchorMax = new Vector2(0.5f, 1f);
        shackleRect.pivot = new Vector2(0.5f, 0f);
        shackleRect.anchoredPosition = new Vector2(0, -50);
        shackleRect.sizeDelta = new Vector2(180, 120);

        CreateRoundedRect("ShackleLeft", shackle.transform, metalLight).GetComponent<RectTransform>().Set(
            new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), Vector2.zero, new Vector2(30, 0));
        CreateRoundedRect("ShackleRight", shackle.transform, metalLight).GetComponent<RectTransform>().Set(
            new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f), Vector2.zero, new Vector2(30, 0));
        CreateRoundedRect("ShackleTop", shackle.transform, metalLight).GetComponent<RectTransform>().Set(
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 50));

        padlockScript.shackle = shackle.transform;

        // === BODY ===
        GameObject body = CreateRoundedRect("Body", padlock.transform, metalDark);
        RectTransform bodyRect = body.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0, 0);
        bodyRect.anchorMax = new Vector2(1, 0);
        bodyRect.pivot = new Vector2(0.5f, 0);
        bodyRect.anchoredPosition = new Vector2(0, 0);
        bodyRect.sizeDelta = new Vector2(0, 300);

        GameObject bodyBorder = CreateRoundedRect("BodyBorder", body.transform, metalLight);
        bodyBorder.GetComponent<RectTransform>().Set(Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-8, -8));

        GameObject bodyInner = CreateRoundedRect("BodyInner", bodyBorder.transform, metalDark);
        bodyInner.GetComponent<RectTransform>().Set(Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-6, -6));

        // === WHEELS CONTAINER ===
        GameObject wheelsContainer = new GameObject("WheelsContainer");
        wheelsContainer.transform.SetParent(body.transform, false);
        RectTransform wheelsRect = wheelsContainer.AddComponent<RectTransform>();
        wheelsRect.anchorMin = new Vector2(0.5f, 0.5f);
        wheelsRect.anchorMax = new Vector2(0.5f, 0.5f);
        wheelsRect.sizeDelta = new Vector2(280, 180);
        wheelsRect.anchoredPosition = new Vector2(0, 20);

        HorizontalLayoutGroup hlg = wheelsContainer.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        padlockScript.wheelsContainer = wheelsRect;

        // === CREATE WHEEL PREFAB ===
        GameObject wheelPrefab = CreateWheelPrefab(gold, goldDark, metalDark);

        // Save prefab
        string prefabPath = "Assets/Prefabs/WheelPrefab.prefab";
        System.IO.Directory.CreateDirectory("Assets/Prefabs");
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(wheelPrefab, prefabPath);
        DestroyImmediate(wheelPrefab);

        padlockScript.wheelPrefab = savedPrefab;

        // Create initial 4 wheels (will be recreated dynamically in WebGL)
        padlockScript.digitDisplays = new TextMeshProUGUI[4];
        padlockScript.upButtons = new Button[4];
        padlockScript.downButtons = new Button[4];

        for (int i = 0; i < 4; i++)
        {
            GameObject wheel = (GameObject)PrefabUtility.InstantiatePrefab(savedPrefab, wheelsContainer.transform);
            wheel.name = "Wheel_" + i;

            // DigitDisplay is now inside DigitArea
            var digitArea = wheel.transform.Find("DigitArea");
            padlockScript.digitDisplays[i] = digitArea.Find("DigitDisplay").GetComponent<TextMeshProUGUI>();
            padlockScript.upButtons[i] = wheel.transform.Find("UpButton").GetComponent<Button>();
            padlockScript.downButtons[i] = wheel.transform.Find("DownButton").GetComponent<Button>();
        }

        // === FEEDBACK TEXT ===
        GameObject feedback = CreateText("FeedbackText", body.transform, "Turn the wheels to set the code", 16, new Color(0.6f, 0.6f, 0.6f));
        RectTransform feedbackRect = feedback.GetComponent<RectTransform>();
        feedbackRect.anchorMin = new Vector2(0, 0);
        feedbackRect.anchorMax = new Vector2(1, 0);
        feedbackRect.pivot = new Vector2(0.5f, 0);
        feedbackRect.anchoredPosition = new Vector2(0, 60);
        feedbackRect.sizeDelta = new Vector2(-20, 30);
        padlockScript.feedbackText = feedback.GetComponent<TextMeshProUGUI>();

        // === UNLOCK BUTTON ===
        GameObject unlockBtn = CreateUnlockButton("UnlockButton", body.transform, gold, goldDark);
        RectTransform unlockRect = unlockBtn.GetComponent<RectTransform>();
        unlockRect.anchorMin = new Vector2(0.5f, 0);
        unlockRect.anchorMax = new Vector2(0.5f, 0);
        unlockRect.pivot = new Vector2(0.5f, 0);
        unlockRect.anchoredPosition = new Vector2(0, 15);
        unlockRect.sizeDelta = new Vector2(200, 45);
        padlockScript.unlockButton = unlockBtn.GetComponent<Button>();

        // === SUCCESS PANEL ===
        GameObject successPanel = CreateRoundedRect("SuccessPanel", padlock.transform, new Color(0.15f, 0.5f, 0.25f, 0.98f));
        RectTransform successRect = successPanel.GetComponent<RectTransform>();
        successRect.anchorMin = Vector2.zero;
        successRect.anchorMax = Vector2.one;
        successRect.sizeDelta = Vector2.zero;

        GameObject successTextObj = CreateText("SuccessText", successPanel.transform, "UNLOCKED!", 42, Color.white);
        successTextObj.GetComponent<RectTransform>().Set(new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0, 60));

        successPanel.SetActive(false);
        padlockScript.successPanel = successPanel;
        padlockScript.successText = successTextObj.GetComponent<TextMeshProUGUI>();

        // Save scene
        string scenePath = "Assets/Scenes/PadlockScene.unity";
        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, scenePath);

        Debug.Log("Padlock scene created! Press Play to test. Solution is: 1234");
        EditorUtility.DisplayDialog("Padlock Created",
            "Scene saved to: " + scenePath + "\n\nPress PLAY to test!\n\nDefault code: 1234\n\nFor WebGL: Build and use host.html",
            "OK");
    }

    static GameObject CreateWheelPrefab(Color wheelColor, Color wheelDark, Color bgColor)
    {
        GameObject wheel = new GameObject("WheelPrefab");
        RectTransform wheelRect = wheel.AddComponent<RectTransform>();
        LayoutElement le = wheel.AddComponent<LayoutElement>();
        le.flexibleWidth = 1;
        le.flexibleHeight = 1;

        Image bg = wheel.AddComponent<Image>();
        bg.color = bgColor;

        // Up button (hidden by default for drag mode, but kept for button mode)
        GameObject upBtn = CreateArrowButton("UpButton", wheel.transform, true, wheelColor, wheelDark);
        upBtn.GetComponent<RectTransform>().Set(new Vector2(0, 0.85f), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        // Draggable digit area - larger for easier dragging
        GameObject digitArea = new GameObject("DigitArea");
        digitArea.transform.SetParent(wheel.transform, false);
        RectTransform digitAreaRect = digitArea.AddComponent<RectTransform>();
        digitAreaRect.Set(new Vector2(0, 0.15f), new Vector2(1, 0.85f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        // Add image for drag detection (raycast target)
        Image digitAreaImg = digitArea.AddComponent<Image>();
        digitAreaImg.color = wheelColor;

        // Digit background (visual styling)
        GameObject digitBg = CreateRoundedRect("DigitBg", digitArea.transform, wheelDark);
        digitBg.GetComponent<RectTransform>().Set(Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-4, -4));
        digitBg.GetComponent<Image>().raycastTarget = false;

        // Inner highlight
        GameObject digitInner = CreateRoundedRect("DigitInner", digitBg.transform, wheelColor);
        digitInner.GetComponent<RectTransform>().Set(Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-4, -4));
        digitInner.GetComponent<Image>().raycastTarget = false;

        // Digit text
        GameObject digitText = CreateText("DigitDisplay", digitArea.transform, "0", 52, Color.black);
        digitText.GetComponent<RectTransform>().Set(Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        digitText.GetComponent<TextMeshProUGUI>().raycastTarget = false;

        // Small drag hint arrows (subtle indicators)
        GameObject hintUp = CreateText("HintUp", digitArea.transform, "\u25B2", 12, new Color(0, 0, 0, 0.3f));
        hintUp.GetComponent<RectTransform>().Set(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -2), new Vector2(20, 15));
        hintUp.GetComponent<TextMeshProUGUI>().raycastTarget = false;

        GameObject hintDown = CreateText("HintDown", digitArea.transform, "\u25BC", 12, new Color(0, 0, 0, 0.3f));
        hintDown.GetComponent<RectTransform>().Set(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 2), new Vector2(20, 15));
        hintDown.GetComponent<TextMeshProUGUI>().raycastTarget = false;

        // Down button (hidden by default for drag mode)
        GameObject downBtn = CreateArrowButton("DownButton", wheel.transform, false, wheelColor, wheelDark);
        downBtn.GetComponent<RectTransform>().Set(new Vector2(0, 0), new Vector2(1, 0.15f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        return wheel;
    }

    static GameObject CreateArrowButton(string name, Transform parent, bool isUp, Color normalColor, Color pressedColor)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        btnObj.AddComponent<RectTransform>();

        Image img = btnObj.AddComponent<Image>();
        img.color = normalColor;

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = new Color(normalColor.r * 1.1f, normalColor.g * 1.1f, normalColor.b * 1.1f);
        colors.pressedColor = pressedColor;
        btn.colors = colors;

        GameObject arrow = CreateText("Arrow", btnObj.transform, isUp ? "\u25B2" : "\u25BC", 24, Color.black);
        arrow.GetComponent<RectTransform>().Set(Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        return btnObj;
    }

    static GameObject CreateUnlockButton(string name, Transform parent, Color normalColor, Color pressedColor)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        btnObj.AddComponent<RectTransform>();

        Image img = btnObj.AddComponent<Image>();
        img.color = normalColor;

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = new Color(normalColor.r * 1.15f, normalColor.g * 1.15f, normalColor.b * 1.15f);
        colors.pressedColor = pressedColor;
        btn.colors = colors;

        GameObject text = CreateText("Text", btnObj.transform, "UNLOCK", 22, Color.black);
        text.GetComponent<RectTransform>().Set(Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        text.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

        return btnObj;
    }

    static GameObject CreateRoundedRect(string name, Transform parent, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        obj.AddComponent<Image>().color = color;
        return obj;
    }

    static GameObject CreateText(string name, Transform parent, string text, int fontSize, Color color)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);
        textObj.AddComponent<RectTransform>();
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        return textObj;
    }
}

// Extension method for cleaner RectTransform setup
public static class RectTransformExtensions
{
    public static void Set(this RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
    }
}
