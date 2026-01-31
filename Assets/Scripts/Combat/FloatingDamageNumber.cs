using UnityEngine;
using TMPro;

public class FloatingDamageNumber : MonoBehaviour
{
    private TextMeshPro tmp;
    private float lifetime = 1f;
    private float elapsed;
    private Vector3 startPos;
    private float floatHeight = 1.5f;
    private Color startColor;

    public void Initialize(float value, bool isDamage, Vector3 worldPos)
    {
        startPos = worldPos + Vector3.up * 0.8f + new Vector3(Random.Range(-0.3f, 0.3f), 0, 0);
        transform.position = startPos;

        tmp = gameObject.AddComponent<TextMeshPro>();
        tmp.text = isDamage ? $"-{value:F0}" : $"+{value:F0}";
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.sortingOrder = 200;
        tmp.rectTransform.sizeDelta = new Vector2(4f, 2f);

        float fontSize = Mathf.Clamp(3f + value * 0.08f, 3f, 10f);
        tmp.fontSize = fontSize;

        if (isDamage)
        {
            tmp.color = Color.white;
            tmp.outlineWidth = 0.3f;
            tmp.outlineColor = Color.black;
        }
        else
        {
            tmp.color = new Color(0.2f, 1f, 0.2f);
            tmp.outlineWidth = 0.3f;
            tmp.outlineColor = new Color(0f, 0.3f, 0f);
        }

        startColor = tmp.color;
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float t = elapsed / lifetime;

        transform.position = startPos + Vector3.up * (floatHeight * t);

        Color c = startColor;
        c.a = 1f - t;
        tmp.color = c;

        if (elapsed >= lifetime)
            Destroy(gameObject);
    }
}
