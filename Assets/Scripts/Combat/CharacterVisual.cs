using UnityEngine;
using Greenveil.Combat;

[RequireComponent(typeof(SpriteRenderer))]
public class CharacterVisual : MonoBehaviour
{
    [SerializeField] private Color characterColor = Color.blue;
    [SerializeField] private Vector2 size = new Vector2(1f, 1.5f);
    [SerializeField] private Sprite characterSprite;

    public Color CharacterColor => characterColor;
    public bool HasAssignedSprite => characterSprite != null;
    private SpriteRenderer mainRenderer;
    private GameObject borderObject;
    private SpriteRenderer borderRenderer;
    private GameObject defendObject;
    private SpriteRenderer defendRenderer;
    private GameObject turnIndicatorObject;
    private SpriteRenderer turnIndicatorRenderer;
    private CombatCharacter character;
    private bool isActive = false;
    private Color displayColor;
    public bool SuppressAutoShake { get; set; }

    // Idle animation
    private Sprite[] animFrames;
    private int frameIndex = 0;
    private float frameTimer = 0f;
    [SerializeField] private float frameDelay = 0.3f;
    private bool animationStopped = false;

    void Awake()
    {
        SetupVisual();
    }

    void SetupVisual()
    {
        mainRenderer = GetComponent<SpriteRenderer>();
        if (mainRenderer == null)
            mainRenderer = gameObject.AddComponent<SpriteRenderer>();

        // Determine the actual display color: use SpriteRenderer scene color if characterColor is white
        displayColor = characterColor;
        if (characterColor == Color.white && mainRenderer.color != Color.white)
            displayColor = mainRenderer.color;

        // Get CombatCharacter early so we can use characterId for sprite loading
        character = GetComponent<CombatCharacter>();

        // Try loading sprite from Resources by characterId if not assigned in Inspector
        if (characterSprite == null && character != null && !string.IsNullOrEmpty(character.CharacterId))
        {
            string path = "Sprites/" + character.CharacterId;
            characterSprite = Resources.Load<Sprite>(path);
            if (characterSprite != null)
                Debug.Log($"[CharacterVisual] {gameObject.name}: Loaded sprite from Resources/{path}");
            else
                Debug.Log($"[CharacterVisual] {gameObject.name}: No sprite found at Resources/{path}, using colored rectangle");
        }

        // Try loading animation frames: {characterId}_0, _1, _2, ...
        if (character != null && !string.IsNullOrEmpty(character.CharacterId))
        {
            var frames = new System.Collections.Generic.List<Sprite>();
            for (int i = 0; i < 10; i++)
            {
                string framePath = "Sprites/" + character.CharacterId + "_" + i;
                Sprite frame = Resources.Load<Sprite>(framePath);
                if (frame != null)
                    frames.Add(frame);
                else
                    break;
            }
            if (frames.Count > 1)
            {
                animFrames = frames.ToArray();
                // Use the first animation frame as the main sprite
                characterSprite = animFrames[0];
                Debug.Log($"[CharacterVisual] {gameObject.name}: Loaded {animFrames.Length} animation frames");
            }
        }

        if (characterSprite != null)
        {
            mainRenderer.sprite = characterSprite;
            mainRenderer.color = Color.white;

            // Scale sprites up to a visible size
            // Small pixel sprites (< 100px) use 2x, larger sprites use 3x
            float spritePixelHeight = characterSprite.rect.height;
            float scaleMultiplier = spritePixelHeight < 100f ? 2f : 3f;
            float targetHeight = size.y * scaleMultiplier;
            float spriteWorldHeight = characterSprite.bounds.size.y;
            if (spriteWorldHeight > 0 && spriteWorldHeight < targetHeight)
            {
                float scaleFactor = targetHeight / spriteWorldHeight;
                transform.localScale = Vector3.one * scaleFactor;
                Debug.Log($"[CharacterVisual] {gameObject.name}: Scaled {scaleFactor:F1}x (sprite {spriteWorldHeight:F2} -> {targetHeight} units)");
            }

            Debug.Log($"[CharacterVisual] {gameObject.name}: Using assigned sprite");
        }
        else
        {
            Sprite mainSprite = CreateColoredSprite(displayColor, 100, 150);
            mainRenderer.sprite = mainSprite;
            mainRenderer.color = Color.white;
            Debug.Log($"[CharacterVisual] {gameObject.name}: Using colored rectangle ({displayColor})");
        }
        mainRenderer.sortingOrder = 1;

        borderObject = new GameObject("Border");
        borderObject.transform.SetParent(transform);
        borderObject.transform.localPosition = Vector3.zero;

        borderRenderer = borderObject.AddComponent<SpriteRenderer>();
        if (characterSprite != null)
        {
            borderRenderer.sprite = characterSprite;
            borderRenderer.color = new Color(1f, 1f, 1f, 0.5f);
        }
        else
        {
            Sprite borderSprite = CreateColoredSprite(Color.white, 110, 160);
            borderRenderer.sprite = borderSprite;
        }
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

        // Turn indicator arrow above character's head (not parented to character to avoid scale issues)
        turnIndicatorObject = new GameObject(gameObject.name + "_TurnIndicator");
        turnIndicatorObject.transform.localScale = new Vector3(0.75f, -0.75f, 1f);
        turnIndicatorRenderer = turnIndicatorObject.AddComponent<SpriteRenderer>();
        turnIndicatorRenderer.sprite = CreateArrowSprite();
        turnIndicatorRenderer.sortingOrder = 5;
        turnIndicatorObject.SetActive(false);

        if (character != null)
        {
            character.OnHealthChanged += OnHealthChanged;
            character.OnCharacterDefeated += OnDefeated;
        }
    }

    Sprite CreateArrowSprite()
    {
        int w = 32;
        int h = 32;
        Texture2D tex = new Texture2D(w, h);
        tex.filterMode = FilterMode.Point;
        Color[] pixels = new Color[w * h];

        // Fill transparent
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.clear;

        Color arrowColor = new Color(1f, 0.9f, 0.2f, 1f); // bright yellow
        Color outlineColor = new Color(0.2f, 0.15f, 0f, 1f); // dark outline

        // Draw a downward-pointing arrow
        // Triangle pointing down: wide at top, narrow at bottom
        int tipY = 4;
        int baseY = 20;
        int centerX = w / 2;

        for (int y = tipY; y <= baseY; y++)
        {
            float t = (float)(y - tipY) / (baseY - tipY);
            int halfWidth = Mathf.RoundToInt(Mathf.Lerp(0, 10, 1f - t));
            for (int x = centerX - halfWidth; x <= centerX + halfWidth; x++)
            {
                if (x >= 0 && x < w)
                {
                    // Outline on edges
                    bool isEdge = (x == centerX - halfWidth || x == centerX + halfWidth || y == baseY || y == tipY);
                    pixels[y * w + x] = isEdge ? outlineColor : arrowColor;
                }
            }
        }

        // Stem on top of the triangle
        int stemLeft = centerX - 3;
        int stemRight = centerX + 3;
        for (int y = baseY; y < 28; y++)
        {
            for (int x = stemLeft; x <= stemRight; x++)
            {
                bool isEdge = (x == stemLeft || x == stemRight || y == 27);
                pixels[y * w + x] = isEdge ? outlineColor : arrowColor;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);
    }

    Sprite CreateColoredSprite(Color color, int width, int height)
    {
        Texture2D texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Point;
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
        if (turnIndicatorObject != null)
            turnIndicatorObject.SetActive(active);
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

        // Idle animation frame cycling
        if (animFrames != null && animFrames.Length > 1 && !animationStopped)
        {
            frameTimer += Time.deltaTime;
            if (frameTimer >= frameDelay)
            {
                frameTimer -= frameDelay;
                frameIndex = (frameIndex + 1) % animFrames.Length;
                mainRenderer.sprite = animFrames[frameIndex];
            }
        }

        // Position and bob the turn indicator above the character in world space
        if (isActive && turnIndicatorObject != null && turnIndicatorObject.activeSelf)
        {
            Bounds worldBounds = mainRenderer.bounds;
            float aboveHead = worldBounds.max.y + 0.5f;
            float bob = Mathf.Sin(Time.time * 3f) * 0.15f;
            turnIndicatorObject.transform.position = new Vector3(worldBounds.center.x, aboveHead + bob, 0);
        }
    }

    public void TriggerHitShake()
    {
        SuppressAutoShake = false;
        StartCoroutine(ShakeDamage());
    }

    void OnHealthChanged(float current, float max)
    {
        if (!SuppressAutoShake)
            StartCoroutine(ShakeDamage());
    }

    void OnDefeated()
    {
        animationStopped = true;
        Color c = characterSprite != null ? Color.white : displayColor;
        mainRenderer.color = new Color(c.r, c.g, c.b, 0.3f);
        if (borderObject != null)
            borderObject.SetActive(false);
        if (turnIndicatorObject != null)
            turnIndicatorObject.SetActive(false);
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
        if (turnIndicatorObject != null)
            Destroy(turnIndicatorObject);
    }
}
