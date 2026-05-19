using UnityEngine;
using System.Collections.Generic;

public class ForestBackground : MonoBehaviour
{
    [SerializeField] private Color groundColor = new Color(0.12f, 0.22f, 0.08f, 1f);
    [SerializeField] private int backgroundSortingOrder = -20;
    [SerializeField] private int treeSortingOrder = -10;
    [SerializeField] private int smallObjectSortingOrder = -5;

    private Sprite[] largeTrees;
    private Sprite[] mediumTrees;
    private Sprite[] smallObjects;
    private Transform bgParent;

    void Start()
    {
        LoadSpritesFromResources();

        bgParent = new GameObject("ForestBackgroundRoot").transform;
        bgParent.SetParent(transform);

        Camera cam = Camera.main;
        if (cam == null) return;

        cam.backgroundColor = groundColor;

        float camH = cam.orthographicSize;
        float camW = camH * cam.aspect;

        CreateGround(camW, camH);
        PlaceLargeTrees(camW, camH);
        PlaceMediumTrees(camW, camH);
        PlaceSmallObjects(camW, camH);

        Debug.Log($"[ForestBackground] Created background with {largeTrees.Length} large, {mediumTrees.Length} medium, {smallObjects.Length} small sprites");
    }

    void LoadSpritesFromResources()
    {
        string basePath = "Sprites/Background/";

        // Large trees
        largeTrees = LoadSpriteGroup(basePath, new string[] {
            "Mega_tree1", "Mega_tree2",
            "Swirling tree1", "Swirling tree2"
        });

        // Medium trees
        mediumTrees = LoadSpriteGroup(basePath, new string[] {
            "Luminous_tree1", "Luminous_tree2", "Luminous_tree3",
            "Curved_tree1", "Curved_tree2",
            "Willow1", "Willow2",
            "White_tree1", "White_tree2"
        });

        // Small objects
        smallObjects = LoadSpriteGroup(basePath, new string[] {
            "Chanterelles1", "Chanterelles2",
            "Beige_green_mushroom1", "Beige_green_mushroom2",
            "White-red_mushroom1", "White-red_mushroom2",
            "Blue-green_balls_tree1", "Light_balls_tree1"
        });
    }

    Sprite[] LoadSpriteGroup(string basePath, string[] names)
    {
        List<Sprite> loaded = new List<Sprite>();
        foreach (string name in names)
        {
            Sprite s = Resources.Load<Sprite>(basePath + name);
            if (s != null)
                loaded.Add(s);
        }
        return loaded.ToArray();
    }

    void CreateGround(float camW, float camH)
    {
        GameObject ground = new GameObject("Ground");
        ground.transform.SetParent(bgParent);

        var sr = ground.AddComponent<SpriteRenderer>();

        // High-res texture so pixels aren't visible when scaled
        int texW = 512;
        int texH = 512;
        Texture2D tex = new Texture2D(texW, texH);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[texW * texH];

        Random.State prevState = Random.state;
        Random.InitState(42);

        // Soft base: Perlin noise for gentle color variation
        for (int y = 0; y < texH; y++)
        {
            for (int x = 0; x < texW; x++)
            {
                // Layer two scales of noise for organic variation
                float n1 = Mathf.PerlinNoise(x * 0.04f, y * 0.04f);
                float n2 = Mathf.PerlinNoise(x * 0.12f + 50f, y * 0.12f + 50f);
                float blend = n1 * 0.7f + n2 * 0.3f;

                // Map noise to a green range
                float g = Mathf.Lerp(0.22f, 0.36f, blend);
                float r = Mathf.Lerp(0.10f, 0.18f, blend);
                float b = Mathf.Lerp(0.06f, 0.12f, blend);

                // Tiny per-pixel jitter so it's not perfectly smooth
                float jitter = Random.Range(-0.015f, 0.015f);
                pixels[y * texW + x] = new Color(
                    Mathf.Clamp01(r + jitter),
                    Mathf.Clamp01(g + jitter),
                    Mathf.Clamp01(b + jitter * 0.5f),
                    1f
                );
            }
        }

        // Grass streaks: many tiny short strokes
        for (int s = 0; s < 2000; s++)
        {
            int sx = Random.Range(0, texW);
            int sy = Random.Range(0, texH);
            int length = Random.Range(2, 5);
            float drift = Random.Range(-0.3f, 0.3f);
            bool bright = Random.value < 0.55f;
            float intensity = Random.Range(0.015f, 0.035f);

            for (int i = 0; i < length; i++)
            {
                int px = sx + Mathf.RoundToInt(i * drift);
                int py = sy + i;
                if (px < 0 || px >= texW || py >= texH) break;

                Color existing = pixels[py * texW + px];
                if (bright)
                    pixels[py * texW + px] = new Color(
                        Mathf.Clamp01(existing.r + intensity * 0.5f),
                        Mathf.Clamp01(existing.g + intensity),
                        Mathf.Clamp01(existing.b + intensity * 0.2f),
                        1f);
                else
                    pixels[py * texW + px] = new Color(
                        Mathf.Clamp01(existing.r - intensity * 0.4f),
                        Mathf.Clamp01(existing.g - intensity * 0.6f),
                        Mathf.Clamp01(existing.b - intensity * 0.2f),
                        1f);
            }
        }

        // A few subtle darker patches for depth (very soft, not blocky)
        for (int p = 0; p < 12; p++)
        {
            float cx = Random.Range(40f, texW - 40f);
            float cy = Random.Range(40f, texH - 40f);
            float radius = Random.Range(20f, 50f);
            float strength = Random.Range(0.02f, 0.04f);
            for (int y = Mathf.Max(0, (int)(cy - radius)); y < Mathf.Min(texH, (int)(cy + radius)); y++)
            {
                for (int x = Mathf.Max(0, (int)(cx - radius)); x < Mathf.Min(texW, (int)(cx + radius)); x++)
                {
                    float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) / radius;
                    if (dist < 1f)
                    {
                        float fade = (1f - dist) * strength;
                        Color c = pixels[y * texW + x];
                        pixels[y * texW + x] = new Color(
                            Mathf.Clamp01(c.r - fade * 0.5f),
                            Mathf.Clamp01(c.g - fade),
                            Mathf.Clamp01(c.b - fade * 0.3f),
                            1f);
                    }
                }
            }
        }

        Random.state = prevState;

        tex.SetPixels(pixels);
        tex.Apply();

        // PPU of 32 means 512px = 16 world units
        float ppu = 32f;
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, texW, texH), new Vector2(0.5f, 0.5f), ppu);
        sr.sortingOrder = backgroundSortingOrder;

        // Scale so texture covers well beyond the camera
        float worldWidth = texW / ppu; // 16 units
        float neededWidth = camW * 3f;
        float scale = neededWidth / worldWidth;
        ground.transform.localScale = new Vector3(scale, scale, 1f);
        ground.transform.position = new Vector3(0, 0, 5);
    }

    void PlaceLargeTrees(float camW, float camH)
    {
        if (largeTrees == null || largeTrees.Length == 0) return;

        Vector2[] positions = {
            new Vector2(-camW - 0.5f, camH + 0.5f),
            new Vector2(camW + 0.5f, camH + 0.5f),
            new Vector2(-camW - 0.5f, -camH - 0.5f),
            new Vector2(camW + 0.5f, -camH - 0.5f),
            new Vector2(-camW + 0.5f, 0f),
            new Vector2(camW - 0.5f, 0f),
            new Vector2(0f, camH + 1f),
        };

        float[] scales = { 2.5f, 2.2f, 2.0f, 2.2f, 1.8f, 2.0f, 2.0f };

        for (int i = 0; i < positions.Length; i++)
        {
            Sprite sprite = largeTrees[i % largeTrees.Length];
            PlaceSprite("LargeTree_" + i, sprite, positions[i], scales[i], treeSortingOrder);
        }
    }

    void PlaceMediumTrees(float camW, float camH)
    {
        if (mediumTrees == null || mediumTrees.Length == 0) return;

        Vector2[] positions = {
            new Vector2(-camW + 1.5f, camH - 0.5f),
            new Vector2(camW - 1.5f, camH - 0.5f),
            new Vector2(-camW + 1.5f, -camH + 0.5f),
            new Vector2(camW - 1.5f, -camH + 0.5f),
            new Vector2(-camW + 0.2f, camH * 0.4f),
            new Vector2(camW - 0.2f, camH * 0.4f),
            new Vector2(-camW + 0.2f, -camH * 0.4f),
            new Vector2(camW - 0.2f, -camH * 0.4f),
            new Vector2(-camW + 3f, camH + 0.3f),
            new Vector2(camW - 3f, camH + 0.3f),
        };

        float[] scales = { 1.5f, 1.3f, 1.5f, 1.3f, 1.6f, 1.5f, 1.5f, 1.6f, 1.3f, 1.3f };

        for (int i = 0; i < positions.Length; i++)
        {
            Sprite sprite = mediumTrees[i % mediumTrees.Length];
            PlaceSprite("MediumTree_" + i, sprite, positions[i], scales[i], treeSortingOrder + 1);
        }
    }

    void PlaceSmallObjects(float camW, float camH)
    {
        if (smallObjects == null || smallObjects.Length == 0) return;

        Vector2[] positions = {
            new Vector2(-camW + 2.5f, camH * 0.2f),
            new Vector2(camW - 2.5f, -camH * 0.2f),
            new Vector2(-camW + 1f, -camH * 0.6f),
            new Vector2(camW - 1f, camH * 0.6f),
            new Vector2(-2f, camH - 0.3f),
            new Vector2(2f, -camH + 0.3f),
            new Vector2(-camW + 3.5f, -camH * 0.5f),
            new Vector2(camW - 3.5f, camH * 0.5f),
        };

        float[] scales = { 0.8f, 1.0f, 0.7f, 0.8f, 1.0f, 0.8f, 0.7f, 1.0f };

        for (int i = 0; i < positions.Length; i++)
        {
            Sprite sprite = smallObjects[i % smallObjects.Length];
            PlaceSprite("SmallObj_" + i, sprite, positions[i], scales[i], smallObjectSortingOrder);
        }
    }

    void PlaceSprite(string name, Sprite sprite, Vector2 position, float scale, int sortOrder)
    {
        if (sprite == null) return;

        GameObject obj = new GameObject(name);
        obj.transform.SetParent(bgParent);
        obj.transform.position = new Vector3(position.x, position.y, 1);
        obj.transform.localScale = Vector3.one * scale;

        var sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = sortOrder;
    }
}
