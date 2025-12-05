using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Greenveil.Combat;
using System.Collections.Generic;


/// </summary>
public class AutoCombatHUD : MonoBehaviour
{
    [Header("HP Bar Colors")]
    [SerializeField] private Color hpColorHigh = new Color(0.2f, 0.9f, 0.2f);    // Green > 50%
    [SerializeField] private Color hpColorMedium = new Color(0.95f, 0.8f, 0.2f); // Yellow 25-50%
    [SerializeField] private Color hpColorLow = new Color(0.95f, 0.2f, 0.2f);    // Red < 25%
    
    [Header("MP Bar Color")]
    [SerializeField] private Color mpColor = new Color(0.2f, 0.6f, 0.95f);       // Blue
    
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
        Debug.Log("🎨 AutoCombatHUD: Awake - Creating HUD...");
        CreateHUD();
    }

    void Start()
    {
        Debug.Log("🎨 AutoCombatHUD: Start - Finding systems...");
        
        turnManager = GetComponent<TurnOrderManager>();
        actionExecutor = GetComponent<CombatActionExecutor>();
        
        Debug.Log($"🎨 AutoCombatHUD: TurnOrderManager found? {turnManager != null}");
        Debug.Log($"🎨 AutoCombatHUD: CombatActionExecutor found? {actionExecutor != null}");
        
        if (turnManager != null)
        {
            turnManager.OnCombatStart += OnCombatStart;
            turnManager.OnTurnStart += OnTurnStart;
            turnManager.OnNewRound += OnNewRound;
            Debug.Log("🎨 AutoCombatHUD: Subscribed to TurnOrderManager events");
        }
        
        if (actionExecutor != null)
        {
            actionExecutor.OnActionExecuted += OnActionExecuted;
            actionExecutor.OnDamageDealt += OnDamageDealt;
            Debug.Log("🎨 AutoCombatHUD: Subscribed to CombatActionExecutor events");
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

        // Info Panel at bottom
        GameObject infoPanel = CreatePanel(canvasObj.transform, new Color(0.05f, 0.05f, 0.05f, 0.95f));
        RectTransform infoRect = infoPanel.GetComponent<RectTransform>();
        infoRect.anchorMin = new Vector2(0, 0);
        infoRect.anchorMax = new Vector2(1, 0);
        infoRect.pivot = new Vector2(0.5f, 0);
        infoRect.anchoredPosition = new Vector2(0, 0);
        infoRect.sizeDelta = new Vector2(0, 180);

        // Round text
        roundText = CreateText(infoPanel.transform, "Round: 1", 24, Color.yellow);
        RectTransform roundRect = roundText.GetComponent<RectTransform>();
        roundRect.anchorMin = new Vector2(0, 1);
        roundRect.anchorMax = new Vector2(0, 1);
        roundRect.pivot = new Vector2(0, 1);
        roundRect.anchoredPosition = new Vector2(30, -15);
        roundRect.sizeDelta = new Vector2(300, 40);
        roundText.alignment = TextAlignmentOptions.TopLeft;

        // Turn text
        turnText = CreateText(infoPanel.transform, "Turn: ...", 24, Color.yellow);
        RectTransform turnRect = turnText.GetComponent<RectTransform>();
        turnRect.anchorMin = new Vector2(1, 1);
        turnRect.anchorMax = new Vector2(1, 1);
        turnRect.pivot = new Vector2(1, 1);
        turnRect.anchoredPosition = new Vector2(-30, -15);
        turnRect.sizeDelta = new Vector2(300, 40);
        turnText.alignment = TextAlignmentOptions.TopRight;

        // Action log
        actionLogText = CreateText(infoPanel.transform, "Combat starting...", 18, Color.white);
        RectTransform logRect = actionLogText.GetComponent<RectTransform>();
        logRect.anchorMin = new Vector2(0, 0);
        logRect.anchorMax = new Vector2(1, 1);
        logRect.offsetMin = new Vector2(30, 30);
        logRect.offsetMax = new Vector2(-30, -60);
        actionLogText.alignment = TextAlignmentOptions.TopLeft;

        Debug.Log("✅ Auto HUD created!");
    }

    public void RegisterCharacter(CombatCharacter character, bool isPlayer)
    {
        if (character == null)
        {
            Debug.LogError("🎨 AutoCombatHUD: Cannot register null character!");
            return;
        }

        Debug.Log($"🎨 AutoCombatHUD: Registering {character.CharacterName} (isPlayer: {isPlayer})");

        if (characterHUDs.ContainsKey(character))
        {
            Debug.LogWarning($"🎨 AutoCombatHUD: {character.CharacterName} already registered!");
            return;
        }

        Vector2 position = isPlayer ? new Vector2(30, -30) : new Vector2(-30, -30);
        Vector2 anchorMin = isPlayer ? new Vector2(0, 1) : new Vector2(1, 1);
        Vector2 anchorMax = anchorMin;
        
        Color panelColor = isPlayer ? new Color(0.1f, 0.15f, 0.3f, 0.95f) : new Color(0.3f, 0.1f, 0.15f, 0.95f);

        GameObject panel = CreatePanel(canvas.transform, panelColor);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = anchorMin;
        panelRect.anchorMax = anchorMax;
        panelRect.pivot = new Vector2(isPlayer ? 0 : 1, 1);
        panelRect.anchoredPosition = position;
        panelRect.sizeDelta = new Vector2(340, 160);

        CharacterHUD hud = new CharacterHUD();
        hud.panel = panel;
        hud.maxHP = character.MaxHealth;
        hud.maxMP = character.MaxMP;

        // Name
        hud.nameText = CreateText(panel.transform, character.CharacterName, 28, Color.white);
        RectTransform nameRect = hud.nameText.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 1);
        nameRect.anchorMax = new Vector2(1, 1);
        nameRect.pivot = new Vector2(0.5f, 1);
        nameRect.anchoredPosition = new Vector2(0, -10);
        nameRect.sizeDelta = new Vector2(-20, 35);
        hud.nameText.alignment = TextAlignmentOptions.Center;
        hud.nameText.fontStyle = FontStyles.Bold;

        // HP Section
        float hpStartY = -55f;
        
        hud.hpText = CreateText(panel.transform, $"HP: {character.CurrentHealth:F0}/{character.MaxHealth:F0}", 18, Color.white);
        RectTransform hpTextRect = hud.hpText.GetComponent<RectTransform>();
        hpTextRect.anchorMin = new Vector2(0, 1);
        hpTextRect.anchorMax = new Vector2(1, 1);
        hpTextRect.pivot = new Vector2(0.5f, 0.5f);
        hpTextRect.anchoredPosition = new Vector2(0, hpStartY);
        hpTextRect.sizeDelta = new Vector2(-40, 25);
        hud.hpText.alignment = TextAlignmentOptions.Center;
        hud.hpText.fontStyle = FontStyles.Bold;

        // HP Bar
        hud.hpBarFill = CreateFillBar(panel.transform, new Vector2(0, hpStartY - 22), hpColorHigh, 300);
        hud.hpBarImage = hud.hpBarFill.GetComponent<Image>();

        // MP Section
        float mpStartY = hpStartY - 50f;
        
        hud.mpText = CreateText(panel.transform, $"MP: {character.CurrentMP:F0}/{character.MaxMP:F0}", 18, Color.white);
        RectTransform mpTextRect = hud.mpText.GetComponent<RectTransform>();
        mpTextRect.anchorMin = new Vector2(0, 1);
        mpTextRect.anchorMax = new Vector2(1, 1);
        mpTextRect.pivot = new Vector2(0.5f, 0.5f);
        mpTextRect.anchoredPosition = new Vector2(0, mpStartY);
        mpTextRect.sizeDelta = new Vector2(-40, 25);
        hud.mpText.alignment = TextAlignmentOptions.Center;
        hud.mpText.fontStyle = FontStyles.Bold;

        // MP Bar
        hud.mpBarFill = CreateFillBar(panel.transform, new Vector2(0, mpStartY - 22), mpColor, 300);
        hud.mpBarImage = hud.mpBarFill.GetComponent<Image>();

        characterHUDs[character] = hud;

        // Subscribe to events
        character.OnHealthChanged += (cur, max) => UpdateHP(character, cur, max);
        character.OnMPChanged += (cur, max) => UpdateMP(character, cur, max);

        Debug.Log($"🎨 AutoCombatHUD: Subscribed to {character.CharacterName}'s health/MP events");

        // Initial update with actual values
        UpdateHP(character, character.CurrentHealth, character.MaxHealth);
        UpdateMP(character, character.CurrentMP, character.MaxMP);

        Debug.Log($"✅ Registered {character.CharacterName} in HUD (HP: {character.CurrentHealth}/{character.MaxHealth}, MP: {character.CurrentMP}/{character.MaxMP})");
    }

    Color GetHPColor(float percent)
    {
        if (percent > 0.5f)
        {
            float t = (percent - 0.5f) / 0.5f;
            return Color.Lerp(hpColorMedium, hpColorHigh, t);
        }
        else if (percent > 0.25f)
        {
            float t = (percent - 0.25f) / 0.25f;
            return Color.Lerp(hpColorLow, hpColorMedium, t);
        }
        else
        {
            return hpColorLow;
        }
    }

    void UpdateHP(CombatCharacter character, float current, float max)
    {
        Debug.Log($"🎨 AutoCombatHUD: UpdateHP called for {character.CharacterName}: {current}/{max}");
        
        if (!characterHUDs.ContainsKey(character))
        {
            Debug.LogWarning($"🎨 AutoCombatHUD: Character {character.CharacterName} not found in HUD dictionary!");
            return;
        }
        
        CharacterHUD hud = characterHUDs[character];
        hud.maxHP = max;
        
        float fillPercent = max > 0 ? Mathf.Clamp01(current / max) : 0;
        
        Vector2 anchorMax = hud.hpBarFill.anchorMax;
        anchorMax.x = fillPercent;
        hud.hpBarFill.anchorMax = anchorMax;
        
        hud.hpBarImage.color = GetHPColor(fillPercent);
        hud.hpText.text = $"HP: {current:F0}/{max:F0}";
        
        Debug.Log($"🎨 AutoCombatHUD: HP bar anchorMax.x = {fillPercent:F2} ({current}/{max})");
    }

    void UpdateMP(CombatCharacter character, float current, float max)
    {
        Debug.Log($"🎨 AutoCombatHUD: UpdateMP called for {character.CharacterName}: {current}/{max}");
        
        if (!characterHUDs.ContainsKey(character))
        {
            Debug.LogWarning($"🎨 AutoCombatHUD: Character {character.CharacterName} not found in HUD dictionary!");
            return;
        }
        
        CharacterHUD hud = characterHUDs[character];
        hud.maxMP = max;
        
        float fillPercent = max > 0 ? Mathf.Clamp01(current / max) : 0;
        
        Vector2 anchorMax = hud.mpBarFill.anchorMax;
        anchorMax.x = fillPercent;
        hud.mpBarFill.anchorMax = anchorMax;
        
        hud.mpText.text = $"MP: {current:F0}/{max:F0}";
        
        Debug.Log($"🎨 AutoCombatHUD: MP bar anchorMax.x = {fillPercent:F2} ({current}/{max})");
    }

    void OnCombatStart()
    {
        Debug.Log("🎨 AutoCombatHUD: OnCombatStart called");
        AddToLog("=== COMBAT START ===");
        roundText.text = "Round: 1";
    }

    void OnTurnStart(CombatCharacter character)
    {
        Debug.Log($"🎨 AutoCombatHUD: OnTurnStart called for {character.CharacterName}");
        turnText.text = $"Turn: {character.CharacterName}";
    }

    void OnNewRound(int round)
    {
        Debug.Log($"🎨 AutoCombatHUD: OnNewRound called - Round {round}");
        roundText.text = $"Round: {round}";
        AddToLog($"--- Round {round} ---");
    }

    void OnActionExecuted(CombatAction action)
    {
        string message = $"{action.user.CharacterName} uses {action.actionType}";
        Debug.Log($"🎨 AutoCombatHUD: OnActionExecuted - {message}");
        AddToLog(message);
    }

    void OnDamageDealt(CombatCharacter target, float damage)
    {
        string message = $"{target.CharacterName} takes {damage:F0} damage!";
        Debug.Log($"🎨 AutoCombatHUD: OnDamageDealt - {message}");
        AddToLog(message);
    }

    public void AddToLog(string message)
    {
        actionLog.Enqueue(message);
        while (actionLog.Count > 5) actionLog.Dequeue();
        actionLogText.text = string.Join("\n", actionLog);
    }

    // Helper methods
    GameObject CreatePanel(Transform parent, Color color)
    {
        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(parent, false);
        Image img = panel.AddComponent<Image>();
        img.color = color;
        
        Outline outline = panel.AddComponent<Outline>();
        outline.effectColor = new Color(0, 0, 0, 0.5f);
        outline.effectDistance = new Vector2(2, -2);
        
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
        tmp.outlineWidth = 0.2f;
        tmp.outlineColor = new Color(0, 0, 0, 0.8f);
        
        return tmp;
    }

    RectTransform CreateFillBar(Transform parent, Vector2 position, Color fillColor, float width = 240)
    {
        GameObject barObj = new GameObject("Bar");
        barObj.transform.SetParent(parent, false);
        RectTransform barRect = barObj.AddComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0.5f, 1f);
        barRect.anchorMax = new Vector2(0.5f, 1f);
        barRect.pivot = new Vector2(0.5f, 0.5f);
        barRect.anchoredPosition = position;
        barRect.sizeDelta = new Vector2(width, 20);

        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(barObj.transform, false);
        RectTransform bgRect = bg.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.15f, 0.15f, 0.15f, 1f);
        
        Outline bgOutline = bg.AddComponent<Outline>();
        bgOutline.effectColor = new Color(0, 0, 0, 1f);
        bgOutline.effectDistance = new Vector2(2, -2);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(barObj.transform, false);
        RectTransform fillRect = fill.AddComponent<RectTransform>();
        
        fillRect.anchorMin = new Vector2(0, 0);
        fillRect.anchorMax = new Vector2(1, 1);
        fillRect.offsetMin = new Vector2(2, 2);
        fillRect.offsetMax = new Vector2(-2, -2);
        
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = fillColor;

        return fillRect;  // Return RectTransform so we can modify anchorMax.x
    }

    class CharacterHUD
    {
        public GameObject panel;
        public TextMeshProUGUI nameText;
        public RectTransform hpBarFill; 
        public Image hpBarImage;
        public TextMeshProUGUI hpText;
        public RectTransform mpBarFill;
        public Image mpBarImage;
        public TextMeshProUGUI mpText;
        public float maxHP;
        public float maxMP;
    }

    void OnDestroy()
    {
        if (canvas != null)
            Destroy(canvas.gameObject);
    }
}