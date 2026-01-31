using UnityEngine;
using Greenveil.Combat;

[RequireComponent(typeof(SpriteRenderer))]
public class CharacterVisual : MonoBehaviour
{
    [SerializeField] private Color characterColor = Color.blue;
    [SerializeField] private Vector2 size = new Vector2(1f, 1.5f);

    public Color CharacterColor => characterColor;
    private SpriteRenderer mainRenderer;
    private GameObject borderObject;
    private SpriteRenderer borderRenderer;
    private GameObject defendObject;
    private SpriteRenderer defendRenderer;
    private CombatCharacter character;
    private bool isActive = false;

    void Awake()
    {
        SetupVisual();
    }

    void SetupVisual()
    {
        mainRenderer = GetComponent<SpriteRenderer>();
        if (mainRenderer == null)
            mainRenderer = gameObject.AddComponent<SpriteRenderer>();

        Sprite mainSprite = CreateColoredSprite(characterColor, 100, 150);
        mainRenderer.sprite = mainSprite;
        mainRenderer.sortingOrder = 1;

        borderObject = new GameObject("Border");
        borderObject.transform.SetParent(transform);
        borderObject.transform.localPosition = Vector3.zero;

        borderRenderer = borderObject.AddComponent<SpriteRenderer>();
        Sprite borderSprite = CreateColoredSprite(Color.white, 110, 160);
        borderRenderer.sprite = borderSprite;
        borderRenderer.sortingOrder = 0;
        borderObject.SetActive(false);

        defendObject = new GameObject("DefendShield");
        defendObject.transform.SetParent(transform);
        defendObject.transform.localPosition = Vector3.zero;
        defendRenderer = defendObject.AddComponent<SpriteRenderer>();
        Sprite defendSprite = CreateColoredSprite(new Color(0.3f, 0.6f, 0.9f), 120, 170);
        defendRenderer.sprite = defendSprite;
        defendRenderer.sortingOrder = -1;
        defendObject.SetActive(false);

        character = GetComponent<CombatCharacter>();
        if (character != null)
        {
            character.OnHealthChanged += OnHealthChanged;
            character.OnCharacterDefeated += OnDefeated;
        }
    }

    Sprite CreateColoredSprite(Color color, int width, int height)
    {
        Texture2D texture = new Texture2D(width, height);
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = color;
        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(
            texture,
            new Rect(0, 0, width, height),
            new Vector2(0.5f, 0.5f),
            100f
        );
    }

    public void SetActive(bool active)
    {
        isActive = active;
        if (borderObject != null)
            borderObject.SetActive(active);
    }

    void Update()
    {
        if (character == null || defendObject == null) return;
        bool defending = character.IsAlive && character.IsDefending;
        defendObject.SetActive(defending);
        if (defending)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 4f);
            defendRenderer.color = new Color(0.3f, 0.6f, 0.9f, pulse);
        }
    }

    void OnHealthChanged(float current, float max)
    {
        StartCoroutine(ShakeDamage());
    }

    void OnDefeated()
    {
        mainRenderer.color = new Color(characterColor.r, characterColor.g, characterColor.b, 0.3f);
        if (borderObject != null)
            borderObject.SetActive(false);
    }

    System.Collections.IEnumerator ShakeDamage()
    {
        Vector3 originalPos = transform.position;
        for (int i = 0; i < 3; i++)
        {
            transform.position = originalPos + new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f), 0);
            yield return new WaitForSeconds(0.05f);
        }
        transform.position = originalPos;
    }

    void OnDestroy()
    {
        if (character != null)
        {
            character.OnHealthChanged -= OnHealthChanged;
            character.OnCharacterDefeated -= OnDefeated;
        }
    }
}
