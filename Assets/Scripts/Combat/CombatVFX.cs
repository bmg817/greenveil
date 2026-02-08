using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Greenveil.Combat;

public class CombatVFX : MonoBehaviour
{
    private CombatActionExecutor actionExecutor;

    // Per-element projectile sprites
    private static Dictionary<ElementType, Sprite> projSprites;
    private static Sprite sparkleSprite;
    private static Sprite impactSprite;

    public static Color GetElementColor(ElementType element)
    {
        switch (element)
        {
            case ElementType.Fire:    return new Color(1f, 0.4f, 0.1f);
            case ElementType.Water:   return new Color(0.2f, 0.55f, 1f);
            case ElementType.Earth:   return new Color(0.75f, 0.5f, 0.2f);
            case ElementType.Air:     return new Color(0.8f, 0.92f, 1f);
            case ElementType.Light:   return new Color(1f, 0.95f, 0.4f);
            case ElementType.Dark:    return new Color(0.55f, 0.15f, 0.8f);
            case ElementType.Nature:  return new Color(0.2f, 0.85f, 0.3f);
            default:                  return new Color(0.9f, 0.9f, 0.9f);
        }
    }

    static float GetTravelDuration(ElementType element)
    {
        switch (element)
        {
            case ElementType.Fire:   return 0.25f;
            case ElementType.Water:  return 0.32f;
            case ElementType.Earth:  return 0.42f;
            case ElementType.Air:    return 0.10f;
            case ElementType.Light:  return 0f; // beam
            case ElementType.Dark:   return 0.50f;
            case ElementType.Nature: return 0.35f;
            default:                 return 0.30f;
        }
    }

    void Awake()
    {
        actionExecutor = GetComponent<CombatActionExecutor>();
        if (actionExecutor != null)
        {
            actionExecutor.OnActionAboutToExecute += OnAboutToExecute;
            actionExecutor.OnActionExecuted += OnActionExecuted;
        }
        CreateAllSprites();
    }

    // ==================== SHAKE TIMING ====================

    void OnAboutToExecute(CombatAction action)
    {
        bool offensive = action.actionType == CombatActionType.Attack || action.actionType == CombatActionType.Skill;
        if (!offensive) return;
        if (action.ability != null && (action.ability.abilityType == AbilityType.HealSkill || action.ability.abilityType == AbilityType.BuffSkill))
            return;
        if (action.targets == null) return;
        foreach (var t in action.targets)
        {
            if (t == null) continue;
            var v = t.GetComponent<CharacterVisual>();
            if (v != null) v.SuppressAutoShake = true;
        }
    }

    void TriggerShake(CombatCharacter target)
    {
        if (target == null) return;
        var v = target.GetComponent<CharacterVisual>();
        if (v != null) v.TriggerHitShake();
    }

    // ==================== SPRITE CREATION ====================

    static void CreateAllSprites()
    {
        if (projSprites != null) return;
        projSprites = new Dictionary<ElementType, Sprite>();
        projSprites[ElementType.Fire]    = CreateFlame();
        projSprites[ElementType.Water]   = CreateDrop();
        projSprites[ElementType.Earth]   = CreateRock();
        projSprites[ElementType.Air]     = CreateSlash();
        projSprites[ElementType.Light]   = CreateStar();
        projSprites[ElementType.Dark]    = CreateWisp();
        projSprites[ElementType.Nature]  = CreateLeaf();
        projSprites[ElementType.Neutral] = CreateCircle(16);
        sparkleSprite = CreateDiamond(10);
        impactSprite  = CreateCircle(24);
    }

    static Sprite GetProjSprite(ElementType e)
    {
        if (projSprites != null && projSprites.ContainsKey(e)) return projSprites[e];
        return projSprites[ElementType.Neutral];
    }

    // Fire: horizontal teardrop (flame shape)
    static Sprite CreateFlame()
    {
        int w = 18, h = 12;
        Texture2D tex = new Texture2D(w, h);
        tex.filterMode = FilterMode.Bilinear;
        Color[] px = new Color[w * h];
        float cx = 6f, cy = h / 2f;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float nx = (x - cx) / (w - cx);
                float ny = (y - cy) / cy;
                float maxY = Mathf.Max(0.05f, 1f - Mathf.Max(0, nx) * 0.7f);
                float r = Mathf.Abs(ny) / maxY;
                if (nx > -0.65f && nx < 1.1f && r < 1f)
                {
                    float a = (1f - r * r) * Mathf.Clamp01(1.2f - nx * 0.4f);
                    float noise = Mathf.PerlinNoise(x * 0.4f + 10f, y * 0.4f) * 0.2f;
                    px[y * w + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(a - (r > 0.5f ? noise : 0f)));
                }
                else px[y * w + x] = Color.clear;
            }
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.33f, 0.5f), 16f);
    }

    // Water: vertical teardrop
    static Sprite CreateDrop()
    {
        int w = 10, h = 16;
        Texture2D tex = new Texture2D(w, h);
        tex.filterMode = FilterMode.Bilinear;
        Color[] px = new Color[w * h];
        float cx = w / 2f, cy = 9f;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float ny = (y - cy) / cy;
                float maxX = Mathf.Max(0.1f, 1f - Mathf.Max(0, -ny) * 0.7f) * (w / 2f);
                float dx = Mathf.Abs(x - cx);
                if (dx < maxX && ny > -1.1f && ny < 0.75f)
                {
                    float r = dx / maxX;
                    float a = (1f - r * r) * Mathf.Clamp01(1f + ny * 0.3f);
                    px[y * w + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(a));
                }
                else px[y * w + x] = Color.clear;
            }
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.55f), 16f);
    }

    // Earth: angular rock
    static Sprite CreateRock()
    {
        int s = 14;
        Texture2D tex = new Texture2D(s, s);
        tex.filterMode = FilterMode.Bilinear;
        Color[] px = new Color[s * s];
        float c = s / 2f;
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float angle = Mathf.Atan2(y - c, x - c);
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                float radius = 5f + 1.5f * Mathf.Sin(angle * 3f + 1f) + 0.8f * Mathf.Sin(angle * 5f);
                if (dist < radius)
                {
                    float edge = 1f - dist / radius;
                    px[y * s + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(edge * 1.5f));
                }
                else px[y * s + x] = Color.clear;
            }
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 16f);
    }

    // Air: horizontal crescent slash
    static Sprite CreateSlash()
    {
        int w = 24, h = 8;
        Texture2D tex = new Texture2D(w, h);
        tex.filterMode = FilterMode.Bilinear;
        Color[] px = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float nx = x / (float)w;
                float centerY = h / 2f + Mathf.Sin(nx * Mathf.PI) * 0.8f;
                float dy = Mathf.Abs(y - centerY);
                float thickness = 2.5f * Mathf.Sin(nx * Mathf.PI);
                if (thickness > 0.1f && dy < thickness)
                {
                    float a = (1f - dy / thickness) * Mathf.Sin(nx * Mathf.PI);
                    px[y * w + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(a));
                }
                else px[y * w + x] = Color.clear;
            }
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
    }

    // Light: 4-pointed star
    static Sprite CreateStar()
    {
        int s = 16;
        Texture2D tex = new Texture2D(s, s);
        tex.filterMode = FilterMode.Bilinear;
        Color[] px = new Color[s * s];
        float c = s / 2f;
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dx = Mathf.Abs(x - c) / c;
                float dy = Mathf.Abs(y - c) / c;
                float cross = Mathf.Min(dx, dy);
                float diamond = (dx + dy);
                float a = 0f;
                if (cross < 0.2f) a = Mathf.Max(a, (0.2f - cross) / 0.2f * (1f - Mathf.Max(dx, dy)));
                if (diamond < 0.7f) a = Mathf.Max(a, 1f - diamond / 0.7f);
                px[y * s + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(a));
            }
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 16f);
    }

    // Dark: wispy circle with tendrils
    static Sprite CreateWisp()
    {
        int s = 16;
        Texture2D tex = new Texture2D(s, s);
        tex.filterMode = FilterMode.Bilinear;
        Color[] px = new Color[s * s];
        float c = s / 2f;
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                float angle = Mathf.Atan2(y - c, x - c);
                float radius = 5f + 2.5f * Mathf.Sin(angle * 2f + 0.7f) * Mathf.Sin(angle * 3f);
                if (dist < radius)
                {
                    float t = dist / radius;
                    px[y * s + x] = new Color(1f, 1f, 1f, Mathf.Pow(1f - t, 0.4f));
                }
                else px[y * s + x] = Color.clear;
            }
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 16f);
    }

    // Nature: leaf / eye shape
    static Sprite CreateLeaf()
    {
        int w = 16, h = 10;
        Texture2D tex = new Texture2D(w, h);
        tex.filterMode = FilterMode.Bilinear;
        Color[] px = new Color[w * h];
        float cx = w / 2f, cy = h / 2f;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float nx = (x - cx) / cx;
                float ny = (y - cy) / cy;
                float d1 = nx * nx + (ny - 0.35f) * (ny - 0.35f);
                float d2 = nx * nx + (ny + 0.35f) * (ny + 0.35f);
                if (d1 < 0.85f && d2 < 0.85f)
                {
                    float edge = Mathf.Min(0.85f - d1, 0.85f - d2);
                    px[y * w + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(edge * 3f));
                }
                else px[y * w + x] = Color.clear;
            }
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
    }

    static Sprite CreateCircle(int size)
    {
        Texture2D tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Bilinear;
        Color[] px = new Color[size * size];
        float c = size / 2f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float t = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / c;
                px[y * size + x] = t < 1f ? new Color(1f, 1f, 1f, 1f - t * t) : Color.clear;
            }
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
    }

    static Sprite CreateDiamond(int size)
    {
        Texture2D tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Bilinear;
        Color[] px = new Color[size * size];
        float c = size / 2f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Abs(x - c) / c + Mathf.Abs(y - c) / c;
                px[y * size + x] = d < 1f ? new Color(1f, 1f, 1f, 1f - d) : Color.clear;
            }
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
    }

    // ==================== EVENT HANDLER ====================

    void OnActionExecuted(CombatAction action)
    {
        if (action.actionType == CombatActionType.Defend)
        {
            StartCoroutine(ShieldEffect(action.user.transform.position));
            return;
        }
        if (action.actionType == CombatActionType.Flee || action.actionType == CombatActionType.Talk)
            return;

        ElementType element = action.ability != null ? action.ability.element : action.user.PrimaryElement;
        bool isSkill = action.ability != null && action.ability.abilityType == AbilityType.DamageSkill;
        bool multiHit = action.ability != null && action.ability.isMultiHit && action.ability.hitCount > 1;

        if (action.ability == null)
        {
            // Fallback basic attack
            if (action.targets != null)
                foreach (var t in action.targets)
                    if (t != null)
                        StartCoroutine(ElementProjectile(action.user, t, element, false, false, 0f));
            return;
        }

        switch (action.ability.abilityType)
        {
            case AbilityType.BasicAttack:
            case AbilityType.DamageSkill:
                if (action.targets != null)
                {
                    float delay = 0f;
                    foreach (var target in action.targets)
                    {
                        if (target == null) continue;
                        if (multiHit)
                        {
                            for (int h = 0; h < action.ability.hitCount; h++)
                                StartCoroutine(ElementProjectile(action.user, target, element, isSkill, true, delay + h * 0.12f));
                        }
                        else
                        {
                            StartCoroutine(ElementProjectile(action.user, target, element, isSkill, false, delay));
                        }
                        delay += 0.08f;
                    }
                }
                break;

            case AbilityType.HealSkill:
                if (action.targets != null)
                    foreach (var t in action.targets)
                        if (t != null) StartCoroutine(HealEffect(t.transform.position));
                break;

            case AbilityType.BuffSkill:
                if (action.targets != null)
                    foreach (var t in action.targets)
                        if (t != null) StartCoroutine(BuffEffect(t.transform.position, element));
                break;

            case AbilityType.DebuffSkill:
                if (action.targets != null)
                {
                    float delay = 0f;
                    foreach (var t in action.targets)
                    {
                        if (t == null) continue;
                        StartCoroutine(DebuffProjectile(action.user, t, element, delay));
                        delay += 0.08f;
                    }
                }
                break;
        }
    }

    // ==================== ELEMENT PROJECTILES ====================

    IEnumerator ElementProjectile(CombatCharacter user, CombatCharacter target, ElementType element, bool isSkill, bool isMultiHitPart, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        Vector3 from = user.transform.position;
        Vector3 to = target.transform.position;
        Color color = GetElementColor(element);
        float duration = GetTravelDuration(element);
        float scale = isMultiHitPart ? 0.5f : (isSkill ? 1.1f : 0.75f);

        // Light is a beam, not a projectile
        if (element == ElementType.Light)
        {
            yield return StartCoroutine(LightBeam(from, to, color, isSkill));
            TriggerShake(target);
            yield break;
        }

        Vector3 dir = (to - from).normalized;
        Vector3 perp = new Vector3(-dir.y, dir.x, 0);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // Create projectile
        GameObject proj = new GameObject("VFX_Proj");
        var sr = proj.AddComponent<SpriteRenderer>();
        sr.sprite = GetProjSprite(element);
        sr.color = color;
        sr.sortingOrder = 100;
        proj.transform.localScale = Vector3.one * scale;
        proj.transform.rotation = Quaternion.Euler(0, 0, angle);

        float elapsed = 0f;
        List<GameObject> trail = new List<GameObject>();
        float trailTimer = 0f;
        float trailRate = element == ElementType.Fire ? 0.02f : (element == ElementType.Air ? 0.01f : 0.035f);

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            Vector3 pos = Vector3.Lerp(from, to, t);

            // Per-element movement
            switch (element)
            {
                case ElementType.Fire:
                    pos.y += Mathf.Sin(t * Mathf.PI) * 0.3f;
                    pos += new Vector3(Random.Range(-0.04f, 0.04f), Random.Range(-0.04f, 0.04f), 0);
                    break;
                case ElementType.Water:
                    pos += perp * Mathf.Sin(t * Mathf.PI * 3f) * 0.4f;
                    break;
                case ElementType.Earth:
                    pos.y += Mathf.Sin(t * Mathf.PI) * 1.5f;
                    break;
                case ElementType.Air:
                    // Straight and fast, slight upward drift
                    pos.y += Mathf.Sin(t * Mathf.PI) * 0.1f;
                    break;
                case ElementType.Dark:
                    pos += perp * Mathf.Sin(t * Mathf.PI * 2f) * 0.5f;
                    break;
                case ElementType.Nature:
                    float spiral = t * Mathf.PI * 5f;
                    pos += perp * Mathf.Sin(spiral) * 0.25f;
                    pos.y += Mathf.Cos(spiral) * 0.15f;
                    break;
                default:
                    pos.y += Mathf.Sin(t * Mathf.PI) * 0.4f;
                    break;
            }

            proj.transform.position = pos;

            // Scale pulse per element
            float p;
            switch (element)
            {
                case ElementType.Fire:
                    p = scale * (0.9f + 0.2f * Random.value);
                    break;
                case ElementType.Dark:
                    p = scale * (0.85f + 0.15f * Mathf.Sin(t * Mathf.PI * 8f));
                    break;
                case ElementType.Earth:
                    p = scale;
                    break;
                default:
                    p = scale * (0.9f + 0.1f * Mathf.Sin(t * Mathf.PI * 4f));
                    break;
            }
            proj.transform.localScale = Vector3.one * p;

            // Trail
            trailTimer += Time.deltaTime;
            if (trailTimer > trailRate)
            {
                trailTimer = 0f;
                GameObject tp = new GameObject("VFX_Trail");
                var tsr = tp.AddComponent<SpriteRenderer>();
                tsr.sprite = GetProjSprite(element);
                Color tc = color;
                tc.a = 0.4f;
                tsr.color = tc;
                tsr.sortingOrder = 99;
                tp.transform.position = pos + new Vector3(Random.Range(-0.06f, 0.06f), Random.Range(-0.06f, 0.06f), 0);
                tp.transform.localScale = Vector3.one * scale * Random.Range(0.2f, 0.4f);
                tp.transform.rotation = Quaternion.Euler(0, 0, angle + Random.Range(-15f, 15f));
                trail.Add(tp);
            }

            // Fade trails
            for (int i = trail.Count - 1; i >= 0; i--)
            {
                if (trail[i] == null) { trail.RemoveAt(i); continue; }
                var tsr2 = trail[i].GetComponent<SpriteRenderer>();
                Color c2 = tsr2.color;
                c2.a -= Time.deltaTime * 3.5f;
                if (c2.a <= 0) { Destroy(trail[i]); trail.RemoveAt(i); }
                else { tsr2.color = c2; trail[i].transform.localScale *= 0.96f; }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(proj);
        foreach (var tp in trail) if (tp != null) Destroy(tp);

        TriggerShake(target);
        StartCoroutine(ElementImpact(to, element, isSkill));
    }

    // ==================== LIGHT BEAM (instant) ====================

    IEnumerator LightBeam(Vector3 from, Vector3 to, Color color, bool isSkill)
    {
        int segCount = isSkill ? 12 : 8;
        List<GameObject> segs = new List<GameObject>();
        Color bright = Color.Lerp(color, Color.white, 0.5f);

        for (int i = 0; i <= segCount; i++)
        {
            float t = i / (float)segCount;
            Vector3 pos = Vector3.Lerp(from, to, t);
            GameObject s = new GameObject("VFX_Beam");
            var sr = s.AddComponent<SpriteRenderer>();
            sr.sprite = projSprites[ElementType.Light];
            sr.color = i == segCount ? bright : color;
            sr.sortingOrder = 100;
            s.transform.position = pos;
            float sz = (isSkill ? 0.5f : 0.35f) * (0.6f + 0.4f * Mathf.Sin(t * Mathf.PI));
            s.transform.localScale = Vector3.one * sz;
            segs.Add(s);
        }

        float dur = 0.25f;
        float elapsed = 0f;
        while (elapsed < dur)
        {
            float t = elapsed / dur;
            for (int i = 0; i < segs.Count; i++)
            {
                if (segs[i] == null) continue;
                var sr = segs[i].GetComponent<SpriteRenderer>();
                Color c = sr.color;
                c.a = 1f - t;
                sr.color = c;
                float shimmer = 1f + 0.3f * Mathf.Sin(Time.time * 20f + i);
                segs[i].transform.localScale *= (1f + Time.deltaTime * shimmer);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        foreach (var s in segs) if (s != null) Destroy(s);
        StartCoroutine(ElementImpact(to, ElementType.Light, isSkill));
    }

    // ==================== ELEMENT IMPACTS ====================

    IEnumerator ElementImpact(Vector3 pos, ElementType element, bool isSkill)
    {
        Color color = GetElementColor(element);
        int sparkCount = isSkill ? 8 : 5;
        bool implode = (element == ElementType.Dark);
        float impactDur = element == ElementType.Air ? 0.2f : (element == ElementType.Dark ? 0.4f : 0.3f);

        // Core flash
        GameObject core = new GameObject("VFX_Impact");
        var csr = core.AddComponent<SpriteRenderer>();
        csr.sprite = impactSprite;
        csr.sortingOrder = 101;
        core.transform.position = pos;

        // Sparks
        List<GameObject> sparks = new List<GameObject>();
        float[] angles = new float[sparkCount];
        float[] speeds = new float[sparkCount];
        for (int i = 0; i < sparkCount; i++)
        {
            angles[i] = i * (360f / sparkCount) * Mathf.Deg2Rad + Random.Range(-0.3f, 0.3f);
            speeds[i] = Random.Range(0.8f, 1.5f);
            GameObject sp = new GameObject("VFX_Spark");
            var ssr = sp.AddComponent<SpriteRenderer>();
            ssr.sprite = sparkleSprite;
            ssr.sortingOrder = 102;
            sp.transform.position = implode ? pos + new Vector3(Mathf.Cos(angles[i]) * 1.5f, Mathf.Sin(angles[i]) * 1.5f, 0) : pos;
            sp.transform.localScale = Vector3.one * (isSkill ? 0.5f : 0.35f);
            sparks.Add(sp);
        }

        // Per-element impact color variants
        Color coreColor, sparkColor;
        switch (element)
        {
            case ElementType.Fire:
                coreColor = new Color(1f, 0.7f, 0.2f);
                sparkColor = new Color(1f, 0.3f, 0f);
                break;
            case ElementType.Water:
                coreColor = new Color(0.4f, 0.7f, 1f);
                sparkColor = new Color(0.1f, 0.4f, 0.9f);
                break;
            case ElementType.Earth:
                coreColor = new Color(0.8f, 0.6f, 0.3f);
                sparkColor = new Color(0.5f, 0.35f, 0.15f);
                break;
            case ElementType.Air:
                coreColor = new Color(0.9f, 0.95f, 1f);
                sparkColor = Color.white;
                break;
            case ElementType.Light:
                coreColor = Color.Lerp(color, Color.white, 0.6f);
                sparkColor = new Color(1f, 0.95f, 0.6f);
                break;
            case ElementType.Dark:
                coreColor = new Color(0.4f, 0.1f, 0.5f);
                sparkColor = new Color(0.6f, 0.2f, 0.7f);
                break;
            case ElementType.Nature:
                coreColor = new Color(0.3f, 0.9f, 0.4f);
                sparkColor = new Color(0.1f, 0.7f, 0.2f);
                break;
            default:
                coreColor = Color.white;
                sparkColor = color;
                break;
        }

        csr.color = coreColor;
        foreach (var sp in sparks)
            sp.GetComponent<SpriteRenderer>().color = sparkColor;

        float elapsed = 0f;
        float coreStartScale = element == ElementType.Light ? 0.8f : 0.2f;

        while (elapsed < impactDur)
        {
            float t = elapsed / impactDur;

            // Core behavior
            switch (element)
            {
                case ElementType.Fire:
                    core.transform.localScale = Vector3.one * (coreStartScale + t * 2f);
                    csr.color = new Color(coreColor.r, coreColor.g - t * 0.3f, coreColor.b - t * 0.2f, 1f - t);
                    break;
                case ElementType.Earth:
                    float shake = (1f - t) * 0.1f;
                    core.transform.position = pos + new Vector3(Random.Range(-shake, shake), 0, 0);
                    core.transform.localScale = Vector3.one * (coreStartScale + t * 1.2f);
                    csr.color = new Color(coreColor.r, coreColor.g, coreColor.b, 1f - t);
                    break;
                case ElementType.Dark:
                    core.transform.localScale = Vector3.one * (1.5f * (1f - t));
                    csr.color = new Color(coreColor.r, coreColor.g, coreColor.b, 0.5f + 0.5f * (1f - t));
                    break;
                case ElementType.Light:
                    core.transform.localScale = Vector3.one * (coreStartScale + t * 1.5f);
                    csr.color = new Color(1f, 1f, 1f, 1f - t * t);
                    break;
                case ElementType.Air:
                    core.transform.localScale = Vector3.one * (coreStartScale + t * 2.5f);
                    core.transform.rotation = Quaternion.Euler(0, 0, t * 360f);
                    csr.color = new Color(coreColor.r, coreColor.g, coreColor.b, 1f - t);
                    break;
                default:
                    core.transform.localScale = Vector3.one * (coreStartScale + t * 1.8f);
                    csr.color = new Color(coreColor.r, coreColor.g, coreColor.b, 1f - t);
                    break;
            }

            // Spark behavior
            for (int i = 0; i < sparks.Count; i++)
            {
                if (sparks[i] == null) continue;
                var ssr = sparks[i].GetComponent<SpriteRenderer>();
                float dist;

                if (implode)
                {
                    dist = 1.5f * (1f - t);
                    sparks[i].transform.position = pos + new Vector3(Mathf.Cos(angles[i]) * dist, Mathf.Sin(angles[i]) * dist, 0);
                }
                else
                {
                    dist = t * speeds[i] * 1.5f;
                    float sparkY = element == ElementType.Fire ? dist * 0.5f : 0f;
                    float sparkGravity = element == ElementType.Earth ? -t * t * 1.5f : 0f;
                    sparks[i].transform.position = pos + new Vector3(Mathf.Cos(angles[i]) * dist, Mathf.Sin(angles[i]) * dist + sparkY + sparkGravity, 0);
                }

                Color sc = sparkColor;
                sc.a = implode ? t : (1f - t);
                ssr.color = sc;
                sparks[i].transform.localScale = Vector3.one * ((isSkill ? 0.5f : 0.35f) * (implode ? t : (1f - t * 0.6f)));

                // Nature: rotate sparks like tumbling leaves
                if (element == ElementType.Nature)
                    sparks[i].transform.rotation = Quaternion.Euler(0, 0, t * 360f * (i % 2 == 0 ? 1 : -1));
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(core);
        foreach (var sp in sparks) if (sp != null) Destroy(sp);
    }

    // ==================== HEAL / BUFF / DEBUFF / SHIELD ====================

    IEnumerator HealEffect(Vector3 position)
    {
        Color color = new Color(0.3f, 1f, 0.4f);
        int count = 8;
        List<GameObject> parts = new List<GameObject>();
        List<Vector3> vels = new List<Vector3>();

        for (int i = 0; i < count; i++)
        {
            GameObject p = new GameObject("VFX_Heal");
            var sr = p.AddComponent<SpriteRenderer>();
            sr.sprite = sparkleSprite;
            sr.color = color;
            sr.sortingOrder = 100;
            p.transform.position = position + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.3f, 0.3f), 0);
            p.transform.localScale = Vector3.one * Random.Range(0.25f, 0.5f);
            parts.Add(p);
            vels.Add(new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(1.2f, 2.5f), 0));
        }

        // Plus sign flash
        GameObject plus = new GameObject("VFX_Plus");
        var psr = plus.AddComponent<SpriteRenderer>();
        psr.sprite = projSprites[ElementType.Light];
        psr.color = new Color(0.5f, 1f, 0.5f, 0.8f);
        psr.sortingOrder = 101;
        plus.transform.position = position;
        plus.transform.localScale = Vector3.one * 0.6f;

        float dur = 0.8f, elapsed = 0f;
        while (elapsed < dur)
        {
            float t = elapsed / dur;
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i] == null) continue;
                parts[i].transform.position += vels[i] * Time.deltaTime;
                var sr = parts[i].GetComponent<SpriteRenderer>();
                sr.color = new Color(color.r, color.g, color.b, (1f - t) * 0.9f);
                float shimmer = 0.3f + 0.2f * Mathf.Sin(Time.time * 10f + i * 2f);
                parts[i].transform.localScale = Vector3.one * shimmer;
            }
            if (plus != null)
            {
                plus.transform.localScale = Vector3.one * (0.6f + t * 0.5f);
                psr.color = new Color(0.5f, 1f, 0.5f, (1f - t) * 0.8f);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        foreach (var p in parts) if (p != null) Destroy(p);
        if (plus != null) Destroy(plus);
    }

    IEnumerator BuffEffect(Vector3 position, ElementType element)
    {
        Color color = Color.Lerp(GetElementColor(element), new Color(1f, 0.9f, 0.3f), 0.3f);
        int count = 6;
        List<GameObject> parts = new List<GameObject>();

        for (int i = 0; i < count; i++)
        {
            GameObject p = new GameObject("VFX_Buff");
            var sr = p.AddComponent<SpriteRenderer>();
            sr.sprite = sparkleSprite;
            sr.color = color;
            sr.sortingOrder = 100;
            p.transform.localScale = Vector3.one * 0.4f;
            parts.Add(p);
        }

        float dur = 0.7f, elapsed = 0f;
        while (elapsed < dur)
        {
            float t = elapsed / dur;
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i] == null) continue;
                float a = (i * (360f / count) + t * 360f) * Mathf.Deg2Rad;
                float r = 0.5f * (1f - t * 0.5f);
                parts[i].transform.position = position + new Vector3(Mathf.Cos(a) * r, -0.3f + t * 2f, 0);
                var sr = parts[i].GetComponent<SpriteRenderer>();
                sr.color = new Color(color.r, color.g, color.b, 1f - t);
                parts[i].transform.localScale = Vector3.one * (0.4f * (1f - t * 0.5f));
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        foreach (var p in parts) if (p != null) Destroy(p);
    }

    IEnumerator DebuffProjectile(CombatCharacter user, CombatCharacter target, ElementType element, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        yield return StartCoroutine(ElementProjectile(user, target, element, false, false, 0f));

        // Dark particles rain down after impact
        Color color = Color.Lerp(GetElementColor(element), new Color(0.3f, 0.1f, 0.3f), 0.4f);
        Vector3 to = target.transform.position;
        int count = 6;
        List<GameObject> parts = new List<GameObject>();
        List<Vector3> vels = new List<Vector3>();

        for (int i = 0; i < count; i++)
        {
            GameObject p = new GameObject("VFX_Debuff");
            var sr = p.AddComponent<SpriteRenderer>();
            sr.sprite = sparkleSprite;
            sr.color = color;
            sr.sortingOrder = 100;
            p.transform.position = to + new Vector3(Random.Range(-0.6f, 0.6f), 1f + Random.Range(0f, 0.5f), 0);
            p.transform.localScale = Vector3.one * Random.Range(0.2f, 0.4f);
            parts.Add(p);
            vels.Add(new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(-2.5f, -1.5f), 0));
        }

        float dur = 0.5f, elapsed = 0f;
        while (elapsed < dur)
        {
            float t = elapsed / dur;
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i] == null) continue;
                parts[i].transform.position += vels[i] * Time.deltaTime;
                var sr = parts[i].GetComponent<SpriteRenderer>();
                sr.color = new Color(color.r, color.g, color.b, 1f - t);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        foreach (var p in parts) if (p != null) Destroy(p);
    }

    IEnumerator ShieldEffect(Vector3 position)
    {
        Color color = new Color(0.3f, 0.6f, 0.95f);
        int count = 8;
        List<GameObject> parts = new List<GameObject>();

        for (int i = 0; i < count; i++)
        {
            float a = i * (360f / count) * Mathf.Deg2Rad;
            GameObject p = new GameObject("VFX_Shield");
            var sr = p.AddComponent<SpriteRenderer>();
            sr.sprite = sparkleSprite;
            sr.color = color;
            sr.sortingOrder = 100;
            p.transform.position = position + new Vector3(Mathf.Cos(a) * 0.8f, Mathf.Sin(a) * 0.8f, 0);
            p.transform.localScale = Vector3.one * 0.5f;
            parts.Add(p);
        }

        float dur = 0.5f, elapsed = 0f;
        while (elapsed < dur)
        {
            float t = elapsed / dur;
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i] == null) continue;
                float a = i * (360f / count) * Mathf.Deg2Rad;
                float r = 0.8f * (1f - t * 0.3f);
                parts[i].transform.position = position + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0);
                var sr = parts[i].GetComponent<SpriteRenderer>();
                float pulse = 0.7f + 0.3f * Mathf.Sin(t * Mathf.PI * 4f);
                sr.color = new Color(color.r, color.g, color.b, pulse * (1f - t));
                parts[i].transform.localScale = Vector3.one * (0.5f * (1f - t * 0.3f));
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        foreach (var p in parts) if (p != null) Destroy(p);
    }

    void OnDestroy()
    {
        if (actionExecutor != null)
        {
            actionExecutor.OnActionAboutToExecute -= OnAboutToExecute;
            actionExecutor.OnActionExecuted -= OnActionExecuted;
        }
    }
}
