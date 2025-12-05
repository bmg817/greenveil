using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Greenveil.Combat;
using System.Collections.Generic;

public class AutoCombatHUD : MonoBehaviour
{
    [Header("HP Bar Colors")]
    [SerializeField] private Color hpColorHigh = new Color(0.2f, 0.9f, 0.2f);
    [SerializeField] private Color hpColorMedium = new Color(0.95f, 0.8f, 0.2f);
    [SerializeField] private Color hpColorLow = new Color(0.95f, 0.2f, 0.2f);
    
    [Header("MP Bar Color")]
    [SerializeField] private Color mpColor = new Color(0.2f, 0.6f, 0.95f);

    private const float BAR_WIDTH = 300f;
    private const float BAR_HEIGHT = 22f;

    private Canvas canvas;
    private Dictionary<CombatCharacter, CharacterHUD> characterHUDs = new Dictionary<CombatCharacter, CharacterHUD>();
    private TextMeshProUGUI roundText;
    private TextMeshProUGUI turnText;
    private TextMeshProUGUI actionLogText;
    private Queue<string> actionLog = new Queue<string>();
    
    private TurnOrderManager turnManager;
    private CombatActionExecutor actionExecutor;

    void Awake()
    {
        CreateHUD();
    }

    void Start()
    {
        turnManager = GetComponent<TurnOrderManager>();
        actionExecutor = GetComponent<CombatActionExecutor>();
        
        if (turnManager != null)
        {
            turnManager.OnCombatStart += OnCombatStart;
            turnManager.OnTurnStart += OnTurnStart;
            turnManager.OnNewRound += OnNewRound;
        }
        
        if (actionExecutor != null)
        {
            actionExecutor.OnActionExecuted += OnActionExecuted;
            actionExecutor.OnDamageDealt += OnDamageDealt;
        }
    }

    void CreateHUD()
    {
        GameObject canvasObj = new GameObject("AutoCombatHUD");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject infoPanel = CreatePanel(canvasObj.transform, new Color(0.05f, 0.05f, 0.05f, 0.95f));
        RectTransform infoRect = infoPanel.GetComponent<RectTransform>();
        infoRect.anchorMin = new Vector2(0, 0);
        infoRect.anchorMax = new Vector2(1, 0);
        infoRect.pivot = new Vector2(0.5f, 0);
        infoRect.anchoredPosition = new Vector2(0, 0);
        infoRect.sizeDelta = new Vector2(0, 180);

        roundText = CreateText(infoPanel.transform, "Round: 1", 28, Color.yellow);
        RectTransform roundRect = roundText.GetComponent<RectTransform>();
        roundRect.anchorMin = new Vector2(0, 1);
        roundRect.anchorMax = new Vector2(0, 1);
        roundRect.pivot = new Vector2(0, 1);
        roundRect.anchoredPosition = new Vector2(30, -15);
        roundRect.sizeDelta = new Vector2(300, 40);
        roundText.alignment = TextAlignmentOptions.TopLeft;

        turnText = CreateText(infoPanel.transform, "Turn: ...", 28, Color.yellow);
        RectTransform turnRect = turnText.GetComponent<RectTransform>();
        turnRect.anchorMin = new Vector2(1, 1);
        turnRect.anchorMax = new Vector2(1, 1);
        turnRect.pivot = new Vector2(1, 1);
        turnRect.anchoredPosition = new Vector2(-30, -15);
        turnRect.sizeDelta = new Vector2(300, 40);
        turnText.alignment = TextAlignmentOptions.TopRight;

        actionLogText = CreateText(infoPanel.transform, "Combat starting...", 22, Color.white);
        RectTransform logRect = actionLogText.GetComponent<RectTransform>();
        logRect.anchorMin = new Vector2(0, 0);
        logRect.anchorMax = new Vector2(1, 1);
        logRect.offsetMin = new Vector2(30, 30);
        logRect.offsetMax = new Vector2(-30, -60);
        actionLogText.alignment = TextAlignmentOptions.TopLeft;
    }

    public void RegisterCharacter(CombatCharacter character, bool isPlayer)
    {
        if (character == null || characterHUDs.ContainsKey(character)) return;

        Debug.Log($"[HUD] Registering {character.CharacterName}, MP: {character.CurrentMP}/{character.MaxMP}");

        Vector2 position = isPlayer ? new Vector2(30, -30) : new Vector2(-30, -30);
        Vector2 anchorMin = isPlayer ? new Vector2(0, 1) : new Vector2(1, 1);
        Color panelColor = isPlayer ? new Color(0.1f, 0.15f, 0.3f, 0.95f) : new Color(0.3f, 0.1f, 0.15f, 0.95f);

        GameObject panel = CreatePanel(canvas.transform, panelColor);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = anchorMin;
        panelRect.anchorMax = anchorMin;
        panelRect.pivot = new Vector2(isPlayer ? 0 : 1, 1);
        panelRect.anchoredPosition = position;
        panelRect.sizeDelta = new Vector2(360, 220);

        CharacterHUD hud = new CharacterHUD();
        hud.panel = panel;

        hud.nameText = CreateText(panel.transform, character.CharacterName, 32, Color.white);
        RectTransform nameRect = hud.nameText.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.5f, 1);
        nameRect.anchorMax = new Vector2(0.5f, 1);
        nameRect.pivot = new Vector2(0.5f, 1);
        nameRect.anchoredPosition = new Vector2(0, -10);
        nameRect.sizeDelta = new Vector2(340, 40);
        hud.nameText.alignment = TextAlignmentOptions.Center;
        hud.nameText.fontStyle = FontStyles.Bold;

        float hpY = -50f;
        hud.hpText = CreateText(panel.transform, "HP:", 22, Color.white);
        RectTransform hpTextRect = hud.hpText.GetComponent<RectTransform>();
        hpTextRect.anchorMin = new Vector2(0.5f, 1);
        hpTextRect.anchorMax = new Vector2(0.5f, 1);
        hpTextRect.pivot = new Vector2(0.5f, 1);
        hpTextRect.anchoredPosition = new Vector2(0, hpY);
        hpTextRect.sizeDelta = new Vector2(340, 28);
        hud.hpText.alignment = TextAlignmentOptions.Center;

        hud.hpBarBg = CreateBarBackground(panel.transform, new Vector2(0, hpY - 40));
        hud.hpBarFill = CreateBarFill(hud.hpBarBg.transform, hpColorHigh);

        float mpY = hpY - 80f;
        hud.mpText = CreateText(panel.transform, "MP:", 22, Color.white);
        RectTransform mpTextRect = hud.mpText.GetComponent<RectTransform>();
        mpTextRect.anchorMin = new Vector2(0.5f, 1);
        mpTextRect.anchorMax = new Vector2(0.5f, 1);
        mpTextRect.pivot = new Vector2(0.5f, 1);
        mpTextRect.anchoredPosition = new Vector2(0, mpY);
        mpTextRect.sizeDelta = new Vector2(340, 28);
        hud.mpText.alignment = TextAlignmentOptions.Center;

        hud.mpBarBg = CreateBarBackground(panel.transform, new Vector2(0, mpY - 40));
        hud.mpBarFill = CreateBarFill(hud.mpBarBg.transform, mpColor);

        characterHUDs[character] = hud;

        character.OnHealthChanged += (cur, max) => {
            Debug.Log($"[HUD EVENT] {character.CharacterName} HP changed: {cur}/{max}");
            UpdateHP(character, cur, max);
        };
        
        character.OnMPChanged += (cur, max) => {
            Debug.Log($"[HUD EVENT] {character.CharacterName} MP changed: {cur}/{max}");
            UpdateMP(character, cur, max);
        };

        UpdateHP(character, character.CurrentHealth, character.MaxHealth);
        UpdateMP(character, character.CurrentMP, character.MaxMP);
        
        Debug.Log($"[HUD] Registered {character.CharacterName} - subscribed to events");
    }

    Color GetHPColor(float percent)
    {
        if (percent > 0.5f) return Color.Lerp(hpColorMedium, hpColorHigh, (percent - 0.5f) * 2f);
        if (percent > 0.25f) return Color.Lerp(hpColorLow, hpColorMedium, (percent - 0.25f) * 4f);
        return hpColorLow;
    }

    void UpdateHP(CombatCharacter character, float current, float max)
    {
        if (!characterHUDs.TryGetValue(character, out CharacterHUD hud)) return;
        
        float percent = max > 0 ? Mathf.Clamp01(current / max) : 0;
        float newWidth = BAR_WIDTH * percent;
        
        hud.hpBarFill.sizeDelta = new Vector2(newWidth, BAR_HEIGHT);
        hud.hpBarFill.GetComponent<Image>().color = GetHPColor(percent);
        hud.hpText.text = $"HP: {current:F0}/{max:F0}";
    }

    void UpdateMP(CombatCharacter character, float current, float max)
    {
        if (!characterHUDs.TryGetValue(character, out CharacterHUD hud))
        {
            Debug.LogWarning($"[HUD] UpdateMP - character not found: {character.CharacterName}");
            return;
        }
        
        float percent = max > 0 ? Mathf.Clamp01(current / max) : 0;
        float newWidth = BAR_WIDTH * percent;
        
        Debug.Log($"[HUD] UpdateMP {character.CharacterName}: {current}/{max} = {percent:P0}, width={newWidth}");
        
        hud.mpBarFill.sizeDelta = new Vector2(newWidth, BAR_HEIGHT);
        hud.mpText.text = $"MP: {current:F0}/{max:F0}";
    }

    void OnCombatStart()
    {
        AddToLog("=== COMBAT START ===");
        roundText.text = "Round: 1";
    }

    void OnTurnStart(CombatCharacter character)
    {
        turnText.text = $"Turn: {character.CharacterName}";
    }

    void OnNewRound(int round)
    {
        roundText.text = $"Round: {round}";
        AddToLog($"--- Round {round} ---");
    }

    void OnActionExecuted(CombatAction action)
    {
        AddToLog($"{action.user.CharacterName} uses {action.actionType}");
    }

    void OnDamageDealt(CombatCharacter target, float damage)
    {
        AddToLog($"{target.CharacterName} takes {damage:F0} damage!");
    }

    public void AddToLog(string message)
    {
        actionLog.Enqueue(message);
        while (actionLog.Count > 5) actionLog.Dequeue();
        actionLogText.text = string.Join("\n", actionLog);
    }

    GameObject CreatePanel(Transform parent, Color color)
    {
        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(parent, false);
        Image img = panel.AddComponent<Image>();
        img.color = color;
        return panel;
    }

    TextMeshProUGUI CreateText(Transform parent, string text, int fontSize, Color color)
    {
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        return tmp;
    }

    RectTransform CreateBarBackground(Transform parent, Vector2 position)
    {
        GameObject bg = new GameObject("BarBg");
        bg.transform.SetParent(parent, false);
        RectTransform rect = bg.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1);
        rect.anchorMax = new Vector2(0.5f, 1);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(BAR_WIDTH + 6, BAR_HEIGHT + 6);
        
        Image img = bg.AddComponent<Image>();
        img.color = new Color(0.1f, 0.1f, 0.1f, 1f);
        
        return rect;
    }

    RectTransform CreateBarFill(Transform parent, Color fillColor)
    {
        GameObject fill = new GameObject("BarFill");
        fill.transform.SetParent(parent, false);
        RectTransform rect = fill.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0.5f);
        rect.anchorMax = new Vector2(0, 0.5f);
        rect.pivot = new Vector2(0, 0.5f);
        rect.anchoredPosition = new Vector2(3, 0);
        rect.sizeDelta = new Vector2(BAR_WIDTH, BAR_HEIGHT);
        
        Image img = fill.AddComponent<Image>();
        img.color = fillColor;
        
        return rect;
    }

    class CharacterHUD
    {
        public GameObject panel;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI hpText;
        public RectTransform hpBarBg;
        public RectTransform hpBarFill;
        public TextMeshProUGUI mpText;
        public RectTransform mpBarBg;
        public RectTransform mpBarFill;
    }

    void OnDestroy()
    {
        if (canvas != null) Destroy(canvas.gameObject);
    }
}