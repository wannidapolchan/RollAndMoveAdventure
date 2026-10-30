using UnityEngine;

/// <summary>World-space text that pops in, rises and fades out.</summary>
public class FloatingText : MonoBehaviour
{
    private const float Duration = 1.4f;
    private float age;
    private TextMesh front;
    private TextMesh back;
    private Color color;

    public static void Spawn(Vector3 position, string text, Color color)
    {
        var go = new GameObject("Floating Text");
        go.transform.position = position + new Vector3(0f, 0.6f, 0f);
        FloatingText ft = go.AddComponent<FloatingText>();
        ft.color = color;
        // A dark copy slightly behind makes the text readable on any tile.
        ft.back = MakeText(go.transform, text, new Vector3(0.04f, -0.04f, 0f), SortOrder.FloatingText);
        ft.front = MakeText(go.transform, text, Vector3.zero, SortOrder.FloatingText + 1);
    }

    private static TextMesh MakeText(Transform parent, string text, Vector3 offset, int order)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = offset;
        var tm = go.AddComponent<TextMesh>();
        tm.font = GameAssets.MainFont;
        tm.text = text;
        tm.fontSize = 80;
        tm.characterSize = 0.065f;
        tm.anchor = TextAnchor.MiddleCenter;
        var mr = go.GetComponent<MeshRenderer>();
        mr.sharedMaterial = GameAssets.MainFont.material;
        mr.sortingOrder = order;
        return tm;
    }

    private void Update()
    {
        age += Time.deltaTime;
        if (age >= Duration)
        {
            Destroy(gameObject);
            return;
        }

        float t = age / Duration;
        transform.position += new Vector3(0f, 0.9f * Time.deltaTime, 0f);
        float pop = age < 0.15f ? Mathf.Lerp(0.4f, 1.15f, age / 0.15f) : Mathf.Lerp(1.15f, 1f, Mathf.Clamp01((age - 0.15f) / 0.1f));
        transform.localScale = Vector3.one * pop;

        float alpha = t < 0.7f ? 1f : 1f - (t - 0.7f) / 0.3f;
        front.color = new Color(color.r, color.g, color.b, alpha);
        back.color = new Color(0.1f, 0.08f, 0.12f, alpha * 0.8f);
    }
}
