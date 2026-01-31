using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Greenveil.Combat;
using System.Collections.Generic;

public class AutoCombatHUD : MonoBehaviour
{
    [SerializeField] private Color hpColorHigh = new Color(0.2f, 0.9f, 0.2f);
    [SerializeField] private Color hpColorMedium = new Color(0.95f, 0.8f, 0.2f);
    [SerializeField] private Color hpColorLow = new Color(0.95f, 0.2f, 0.2f);
    [SerializeField] private Color mpColor = new Color(0.2f, 0.6f, 0.95f);

    private const float BAR_WIDTH = 280f;
    private const float BAR_HEIGHT = 18f;
    private const float PANEL_WIDTH = 320f;
    private const float PANEL_HEIGHT = 140f;
    private const float PANEL_SPACING = 10f;

    private Canvas canvas;
    private Dictionary<CombatCharacter, CharacterHUD> characterHUDs = new Dictionary<CombatCharacter, CharacterHUD>();
    private TextMeshProUGUI roundText;
    private TextMeshProUGUI turnText;
    private TextMeshProUGUI actionLogText;
    private Queue<string> actionLog = new Queue<string>();

    private TurnOrderManager turnManager;
    private CombatActionExecutor actionExecutor;
    private int playerCount;
    private int enemyCount;

    private const int TURN_ORDER_COUNT = 10;
    private const float TURN_ENTRY_HEIGHT = 30f;
    private GameObject turnOrderPanel;
    private List<TurnOrderEntry> turnOrderEntries = new List<TurnOrderEntry>();
    private Dictionary<CombatCharacter, bool> characterIsPlayer = new Dictionary<CombatCharacter, bool>();
    private Dictionary<CombatCharacter, Color> characterColors = new Dictionary<CombatCharacter, Color>();

    private const int MAX_SKILL_ENTRIES = 7;
    private const float SKILL_ENTRY_WIDTH = 200f;
    private const float SKILL_ENTRY_HEIGHT = 50f;
    private const float SKILL_ENTRY_SPACING = 8f;
    private GameObject skillsBar;
    private List<SkillBarEntry> skillBarEntries = new List<SkillBarEntry>();

    void Awake()
    {
        CreateHUD();

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
        infoRect.sizeDelta = new Vector2(0, 160);

        roundText = CreateText(infoPanel.transform, "Round: 1", 26, Color.yellow);
        SetRectAnchored(roundText, new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -10), new Vector2(250, 32));
        roundText.alignment = TextAlignmentOptions.TopLeft;

        turnText = CreateText(infoPanel.transform, "Turn: ...", 26, Color.yellow);
        SetRectAnchored(turnText, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -10), new Vector2(300, 32), new Vector2(1, 1));
        turnText.alignment = TextAlignmentOptions.TopRight;

        actionLogText = CreateText(infoPanel.transform, "Combat starting...", 20, Color.white);
        RectTransform logRect = actionLogText.GetComponent<RectTransform>();
        logRect.anchorMin = new Vector2(0, 0);
        logRect.anchorMax = new Vector2(1, 1);
        logRect.offsetMin = new Vector2(20, 20);
        logRect.offsetMax = new Vector2(-20, -48);
        actionLogText.alignment = TextAlignmentOptions.TopLeft;

        CreateTurnOrderPanel();
        CreateSkillsBar();
    }

    void CreateTurnOrderPanel()
    {
        float panelHeight = 32f + TURN_ORDER_COUNT * TURN_ENTRY_HEIGHT + 10f;

        turnOrderPanel = CreatePanel(canvas.transform, new Color(0.05f, 0.05f, 0.05f, 0.9f));
        RectTransform panelRect = turnOrderPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 1);
        panelRect.anchorMax = new Vector2(0, 1);
        panelRect.pivot = new Vector2(0, 1);
        panelRect.anchoredPosition = new Vector2(350, -20);
        panelRect.sizeDelta = new Vector2(220, panelHeight);

        var header = CreateText(turnOrderPanel.transform, "Turn Order", 20, Color.yellow);
        SetRectAnchored(header, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -4), new Vector2(200, 28));
        header.alignment = TextAlignmentOptions.Center;
        header.fontStyle = FontStyles.Bold;

        for (int i = 0; i < TURN_ORDER_COUNT; i++)
        {
            float yPos = -34f - i * TURN_ENTRY_HEIGHT;
            var entry = new TurnOrderEntry();

            entry.row = new GameObject("TurnEntry");
            entry.row.transform.SetParent(turnOrderPanel.transform, false);
            RectTransform rowRect = entry.row.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0, 1);
            rowRect.anchorMax = new Vector2(0, 1);
            rowRect.pivot = new Vector2(0, 1);
            rowRect.anchoredPosition = new Vector2(0, yPos);
            rowRect.sizeDelta = new Vector2(220, TURN_ENTRY_HEIGHT);

            GameObject indicatorObj = new GameObject("Indicator");
            indicatorObj.transform.SetParent(entry.row.transform, false);
            entry.indicator = indicatorObj.AddComponent<Image>();
            RectTransform indRect = indicatorObj.GetComponent<RectTransform>();
            indRect.anchorMin = new Vector2(0, 0.5f);
            indRect.anchorMax = new Vector2(0, 0.5f);
            indRect.pivot = new Vector2(0, 0.5f);
            indRect.anchoredPosition = new Vector2(8, 0);
            indRect.sizeDelta = new Vector2(14, 14);

            entry.marker = CreateText(entry.row.transform, "", 16, Color.yellow);
            SetRectAnchored(entry.marker, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(28, 0), new Vector2(16, TURN_ENTRY_HEIGHT), new Vector2(0, 0.5f));

            entry.nameText = CreateText(entry.row.transform, "", 16, Color.white);
            SetRectAnchored(entry.nameText, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(44, 0), new Vector2(170, TURN_ENTRY_HEIGHT), new Vector2(0, 0.5f));
            entry.nameText.alignment = TextAlignmentOptions.Left;

            turnOrderEntries.Add(entry);
        }
    }

    public void RegisterCharacter(CombatCharacter character, bool isPlayer)
    {
        if (character == null || characterHUDs.ContainsKey(character)) return;

        int index = isPlayer ? playerCount++ : enemyCount++;
        float yOffset = -20f - index * (PANEL_HEIGHT + PANEL_SPACING);

        Vector2 position = isPlayer ? new Vector2(20, yOffset) : new Vector2(-20, yOffset);
        Vector2 anchor = isPlayer ? new Vector2(0, 1) : new Vector2(1, 1);
        Color panelColor = isPlayer
            ? new Color(0.1f, 0.15f, 0.3f, 1f)
            : new Color(0.3f, 0.1f, 0.15f, 1f);

        GameObject panel = CreatePanel(canvas.transform, panelColor);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = anchor;
        panelRect.anchorMax = anchor;
        panelRect.pivot = new Vector2(isPlayer ? 0 : 1, 1);
        panelRect.anchoredPosition = position;
        panelRect.sizeDelta = new Vector2(PANEL_WIDTH, PANEL_HEIGHT);

        CharacterHUD hud = new CharacterHUD();
        hud.panel = panel;
        hud.panelImage = panel.GetComponent<Image>();
        hud.normalColor = panelColor;

        var visual = character.GetComponent<CharacterVisual>();
        Color charColor = visual != null ? visual.CharacterColor : (isPlayer ? new Color(0.3f, 0.5f, 0.9f) : new Color(0.9f, 0.3f, 0.3f));
        characterColors[character] = charColor;

        hud.nameText = CreateText(panel.transform, character.CharacterName, 24, Color.white);
        SetRectAnchored(hud.nameText, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(PANEL_WIDTH - 20, 28));
        hud.nameText.alignment = TextAlignmentOptions.Center;
        hud.nameText.fontStyle = FontStyles.Bold;

        hud.hpText = CreateText(panel.transform, "HP:", 18, Color.white);
        SetRectAnchored(hud.hpText, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -36), new Vector2(PANEL_WIDTH - 20, 22));
        hud.hpText.alignment = TextAlignmentOptions.Center;

        hud.hpBarBg = CreateBarBackground(panel.transform, new Vector2(0, -60));
        hud.hpBarFill = CreateBarFill(hud.hpBarBg.transform, hpColorHigh);

        hud.mpText = CreateText(panel.transform, "MP:", 18, Color.white);
        SetRectAnchored(hud.mpText, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -84), new Vector2(PANEL_WIDTH - 20, 22));
        hud.mpText.alignment = TextAlignmentOptions.Center;

        hud.mpBarBg = CreateBarBackground(panel.transform, new Vector2(0, -108));
        hud.mpBarFill = CreateBarFill(hud.mpBarBg.transform, mpColor);

        hud.defendText = CreateText(panel.transform, "DEFENDING", 16, new Color(1f, 0.85f, 0.2f));
        SetRectAnchored(hud.defendText, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 4), new Vector2(PANEL_WIDTH - 20, 20), new Vector2(0.5f, 0));
        hud.defendText.alignment = TextAlignmentOptions.Center;
        hud.defendText.fontStyle = FontStyles.Bold;
        hud.defendText.gameObject.SetActive(false);

        characterHUDs[character] = hud;

        character.OnHealthChanged += (cur, max) => UpdateHP(character, cur, max);
        character.OnMPChanged += (cur, max) => UpdateMP(character, cur, max);
        character.OnDamageTaken += (dmg) => SpawnFloatingNumber(character.transform.position, dmg, true);
        character.OnHealReceived += (amt) => SpawnFloatingNumber(character.transform.position, amt, false);

        characterIsPlayer[character] = isPlayer;

        UpdateHP(character, character.CurrentHealth, character.MaxHealth);
        UpdateMP(character, character.CurrentMP, character.MaxMP);
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
        hud.hpBarFill.sizeDelta = new Vector2(BAR_WIDTH * percent, BAR_HEIGHT);
        hud.hpBarFill.GetComponent<Image>().color = GetHPColor(percent);
        hud.hpText.text = $"HP: {current:F0}/{max:F0}";
    }

    void UpdateMP(CombatCharacter character, float current, float max)
    {
        if (!characterHUDs.TryGetValue(character, out CharacterHUD hud)) return;

        float percent = max > 0 ? Mathf.Clamp01(current / max) : 0;
        hud.mpBarFill.sizeDelta = new Vector2(BAR_WIDTH * percent, BAR_HEIGHT);
        hud.mpText.text = $"MP: {current:F0}/{max:F0}";
    }

    void SpawnFloatingNumber(Vector3 worldPos, float value, bool isDamage)
    {
        GameObject obj = new GameObject("FloatingNumber");
        var floater = obj.AddComponent<FloatingDamageNumber>();
        floater.Initialize(value, isDamage, worldPos);
    }

    void RefreshTurnOrder()
    {
        if (turnManager == null) return;

        var upcoming = turnManager.GetUpcomingTurns(TURN_ORDER_COUNT);

        for (int i = 0; i < turnOrderEntries.Count; i++)
        {
            var entry = turnOrderEntries[i];

            if (i < upcoming.Count)
            {
                entry.row.SetActive(true);
                var character = upcoming[i];
                bool isPlayer = characterIsPlayer.ContainsKey(character) && characterIsPlayer[character];

                entry.indicator.color = GetIndicatorColor(character, isPlayer);

                bool isCurrent = (i == 0);
                entry.nameText.text = character.CharacterName;
                entry.nameText.fontStyle = isCurrent ? FontStyles.Bold : FontStyles.Normal;
                entry.nameText.color = isCurrent ? Color.yellow : Color.white;
                entry.marker.text = isCurrent ? ">" : "";
                entry.marker.color = Color.yellow;
            }
            else
            {
                entry.row.SetActive(false);
            }
        }
    }

    void CreateSkillsBar()
    {
        skillsBar = CreatePanel(canvas.transform, new Color(0.08f, 0.08f, 0.12f, 0.95f));
        RectTransform barRect = skillsBar.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0.5f, 0);
        barRect.anchorMax = new Vector2(0.5f, 0);
        barRect.pivot = new Vector2(0.5f, 0);
        barRect.anchoredPosition = new Vector2(0, 160);
        float totalWidth = MAX_SKILL_ENTRIES * SKILL_ENTRY_WIDTH + (MAX_SKILL_ENTRIES - 1) * SKILL_ENTRY_SPACING + 20f;
        barRect.sizeDelta = new Vector2(totalWidth, SKILL_ENTRY_HEIGHT + 10f);

        for (int i = 0; i < MAX_SKILL_ENTRIES; i++)
        {
            float xPos = 10f + i * (SKILL_ENTRY_WIDTH + SKILL_ENTRY_SPACING);
            var entry = new SkillBarEntry();

            entry.panel = CreatePanel(skillsBar.transform, new Color(0.15f, 0.15f, 0.22f, 1f));
            RectTransform entryRect = entry.panel.GetComponent<RectTransform>();
            entryRect.anchorMin = new Vector2(0, 0.5f);
            entryRect.anchorMax = new Vector2(0, 0.5f);
            entryRect.pivot = new Vector2(0, 0.5f);
            entryRect.anchoredPosition = new Vector2(xPos, 0);
            entryRect.sizeDelta = new Vector2(SKILL_ENTRY_WIDTH, SKILL_ENTRY_HEIGHT);

            entry.background = entry.panel.GetComponent<Image>();
            entry.normalBg = new Color(0.15f, 0.15f, 0.22f, 1f);

            entry.keyText = CreateText(entry.panel.transform, "", 14, Color.yellow);
            SetRectAnchored(entry.keyText, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -2), new Vector2(SKILL_ENTRY_WIDTH - 10, 18));
            entry.keyText.alignment = TextAlignmentOptions.Center;
            entry.keyText.fontStyle = FontStyles.Bold;

            entry.nameText = CreateText(entry.panel.transform, "", 14, Color.white);
            SetRectAnchored(entry.nameText, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 4), new Vector2(SKILL_ENTRY_WIDTH - 10, 18), new Vector2(0.5f, 0));
            entry.nameText.alignment = TextAlignmentOptions.Center;

            entry.panel.SetActive(false);
            skillBarEntries.Add(entry);
        }

        skillsBar.SetActive(false);
    }

    public void ShowSkillsBar(CombatCharacter character)
    {
        int idx = 0;

        if (character.BasicAttack != null)
        {
            SetSkillEntry(idx, "Space", character.BasicAttack.AbilityName, "", true);
            idx++;
        }

        if (character.Skills != null)
        {
            for (int i = 0; i < character.Skills.Length && idx < MAX_SKILL_ENTRIES - 2; i++)
            {
                var skill = character.Skills[i];
                bool canUse = skill.CanUse(character);
                string cost = "";
                if (skill.CostsMP)
                {
                    float mpCost = character.MaxMP * (skill.MPCostPercent / 100f);
                    cost = $"{mpCost:F0} MP";
                }
                SetSkillEntry(idx, (i + 1).ToString(), skill.AbilityName, cost, canUse);
                idx++;
            }
        }

        SetSkillEntry(idx, "D", "Defend", "", true);
        idx++;
        SetSkillEntry(idx, "F", "Flee", "", true);
        idx++;

        for (int i = idx; i < MAX_SKILL_ENTRIES; i++)
            skillBarEntries[i].panel.SetActive(false);

        float activeWidth = idx * SKILL_ENTRY_WIDTH + (idx - 1) * SKILL_ENTRY_SPACING + 20f;
        skillsBar.GetComponent<RectTransform>().sizeDelta = new Vector2(activeWidth, SKILL_ENTRY_HEIGHT + 10f);

        for (int i = 0; i < idx; i++)
        {
            float xPos = 10f + i * (SKILL_ENTRY_WIDTH + SKILL_ENTRY_SPACING);
            skillBarEntries[i].panel.GetComponent<RectTransform>().anchoredPosition = new Vector2(xPos, 0);
        }

        skillsBar.SetActive(true);
    }

    void SetSkillEntry(int index, string key, string name, string cost, bool available)
    {
        var entry = skillBarEntries[index];
        entry.panel.SetActive(true);
        entry.background.color = entry.normalBg;

        string displayName = string.IsNullOrEmpty(cost) ? name : $"{name} ({cost})";
        entry.keyText.text = $"[{key}]";
        entry.nameText.text = displayName;

        Color textColor = available ? Color.white : new Color(0.4f, 0.4f, 0.4f);
        entry.nameText.color = textColor;
        entry.keyText.color = available ? Color.yellow : new Color(0.4f, 0.4f, 0.3f);
    }

    public void HideSkillsBar()
    {
        if (skillsBar != null)
            skillsBar.SetActive(false);
    }

    public void HighlightAction(int index)
    {
        if (index < 0 || index >= skillBarEntries.Count) return;
        if (!skillBarEntries[index].panel.activeSelf) return;
        skillBarEntries[index].background.color = new Color(0.6f, 0.5f, 0.1f, 1f);
    }

    void OnCombatStart()
    {
        AddToLog("=== COMBAT START ===");
        roundText.text = "Round: 1";
        RefreshTurnOrder();
    }

    void OnTurnStart(CombatCharacter character)
    {
        turnText.text = $"Turn: {character.CharacterName}";
        RefreshTurnOrder();
    }

    void OnNewRound(int round)
    {
        roundText.text = $"Round: {round}";
        AddToLog($"--- Round {round} ---");
        RefreshTurnOrder();
    }

    void OnActionExecuted(CombatAction action)
    {
        string abilityName = action.ability != null ? action.ability.AbilityName : action.actionType.ToString();
        AddToLog($"{action.user.CharacterName} uses {abilityName}");
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

    void SetRectAnchored(TextMeshProUGUI tmp, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Vector2? pivot = null)
    {
        RectTransform rect = tmp.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot ?? new Vector2(0.5f, 1);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
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
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Truncate;
        return tmp;
    }

    RectTransform CreateBarBackground(Transform parent, Vector2 position)
    {
        GameObject bg = new GameObject("BarBg");
        bg.transform.SetParent(parent, false);
        RectTransform rect = bg.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1);
        rect.anchorMax = new Vector2(0.5f, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(BAR_WIDTH + 6, BAR_HEIGHT + 6);

        Image img = bg.AddComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.15f, 1f);

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
        public Image panelImage;
        public Color normalColor;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI hpText;
        public RectTransform hpBarBg;
        public RectTransform hpBarFill;
        public TextMeshProUGUI mpText;
        public RectTransform mpBarBg;
        public RectTransform mpBarFill;
        public TextMeshProUGUI defendText;
    }

    class TurnOrderEntry
    {
        public GameObject row;
        public Image indicator;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI marker;
    }

    class SkillBarEntry
    {
        public GameObject panel;
        public Image background;
        public TextMeshProUGUI keyText;
        public TextMeshProUGUI nameText;
        public Color normalBg;
    }

    Color GetIndicatorColor(CombatCharacter character, bool isPlayer)
    {
        if (!isPlayer) return new Color(0.9f, 0.3f, 0.3f);

        switch (character.PrimaryElement)
        {
            case ElementType.Fire: return new Color(0.3f, 0.5f, 0.95f);
            case ElementType.Nature: return new Color(0.2f, 0.85f, 0.2f);
            case ElementType.Earth: return new Color(0.75f, 0.55f, 0.2f);
            case ElementType.Water: return new Color(0.2f, 0.7f, 0.9f);
            case ElementType.Air: return new Color(0.7f, 0.8f, 0.95f);
            case ElementType.Light: return new Color(0.95f, 0.9f, 0.4f);
            case ElementType.Dark: return new Color(0.55f, 0.2f, 0.65f);
            default: return new Color(0.5f, 0.5f, 0.8f);
        }
    }

    private static readonly Color defendGlow = new Color(0.3f, 0.6f, 0.9f, 1f);

    void Update()
    {
        foreach (var kvp in characterHUDs)
        {
            if (kvp.Key == null) continue;
            var hud = kvp.Value;
            bool defending = kvp.Key.IsAlive && kvp.Key.IsDefending;

            if (defending)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 5f);
                hud.panelImage.color = Color.Lerp(hud.normalColor, defendGlow, pulse);
            }
            else
            {
                hud.panelImage.color = hud.normalColor;
            }

            if (hud.defendText != null)
                hud.defendText.gameObject.SetActive(defending);
        }
    }

    void OnDestroy()
    {
        if (canvas != null) Destroy(canvas.gameObject);
    }
}
