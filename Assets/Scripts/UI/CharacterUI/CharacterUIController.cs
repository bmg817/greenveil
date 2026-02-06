using UnityEngine;
using UnityEngine.UIElements;

public class CharacterUIController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    private VisualElement root;

    private VisualElement healthBar;
    private VisualElement mpBar;
    private VisualElement statusEffectsContainer;

    private float maxHP;
    private float maxMP;

    private void Awake()
    {
        root = uiDocument.rootVisualElement;

        healthBar = root.Q<VisualElement>("characterHealthBar");
        mpBar = root.Q<VisualElement>("characterMPBar");
        statusEffectsContainer = root.Q<VisualElement>("characterStatusEffects");
    }

    public void Initialize(float maxHP, float maxMP)
    {
        this.maxHP = maxHP;
        this.maxMP = maxMP;

        SetHealth(maxHP);
        SetMP(maxMP);
    }

    public void SetHealth(float currentHP)
    {
        float percent = Mathf.Clamp01(currentHP / maxHP);
        healthBar.style.width = Length.Percent(percent * 100f);
    }

    public void SetMP(float currentMP)
    {
        float percent = Mathf.Clamp01(currentMP / maxMP);
        mpBar.style.width = Length.Percent(percent * 100f);
    }

    public void UpdateStatusEffects(StatusEffectData[] effects)
    {
        statusEffectsContainer.Clear();

        foreach (var effect in effects)
        {
            var icon = new VisualElement();
            icon.AddToClassList("status-icon");
            icon.style.backgroundImage = new StyleBackground(effect.icon);
            statusEffectsContainer.Add(icon);
        }
    }
}