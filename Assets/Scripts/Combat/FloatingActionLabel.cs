using UnityEngine;
using TMPro;

public class FloatingActionLabel : MonoBehaviour
{
    private TextMeshPro tmp;
    private float lifetime = 1.5f;
    private float elapsed;
    private Vector3 startPos;
    private Color startColor;

    public void Initialize(string actionName, Vector3 worldPos, Color color)
    {
        startPos = worldPos + Vector3.up * 1.5f;
        transform.position = startPos;

        tmp = gameObject.AddComponent<TextMeshPro>();
        tmp.text = actionName;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.sortingOrder = 199;
        tmp.rectTransform.sizeDelta = new Vector2(6f, 2f);
        tmp.fontSize = 3.5f;
        tmp.color = color;
        tmp.outlineWidth = 0.25f;
        tmp.outlineColor = new Color(0f, 0f, 0f, 0.8f);
        tmp.fontStyle = FontStyles.Bold;

        startColor = color;
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float t = elapsed / lifetime;

        transform.position = startPos + Vector3.up * (0.5f * t);

        Color c = startColor;
        c.a = t < 0.5f ? 1f : 1f - (t - 0.5f) * 2f;
        tmp.color = c;

        if (elapsed >= lifetime)
            Destroy(gameObject);
    }
}
