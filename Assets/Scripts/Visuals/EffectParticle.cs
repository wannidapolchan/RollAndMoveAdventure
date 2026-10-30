using UnityEngine;

/// <summary>One moving sprite in an effect. Moves, spins, fades, then destroys itself.</summary>
public class EffectParticle : MonoBehaviour
{
    public Vector3 Velocity;
    public float Gravity;
    public float Life;
    public float Spin;
    public float Wobble;     // side to side swing, for confetti

    private float age;
    private SpriteRenderer sr;
    private Color baseColor;
    private Vector3 baseScale;

    public static EffectParticle Spawn(Vector3 position, string image, Color color, Vector3 velocity,
                                       float gravity, float size, float life)
    {
        var go = new GameObject("Effect");
        go.transform.position = position;
        var sr = GameAssets.AddSpriteRenderer(go);
        Texture2D tex = GameAssets.Texture(image);
        sr.sprite = GameAssets.Sprite(image, tex.width); // 1 world unit wide, then scaled
        sr.color = color;
        sr.sortingOrder = SortOrder.Effects;

        EffectParticle p = go.AddComponent<EffectParticle>();
        p.sr = sr;
        p.baseColor = color;
        p.Velocity = velocity;
        p.Gravity = gravity;
        p.Life = life;
        p.baseScale = Vector3.one * size;
        go.transform.localScale = p.baseScale;
        return p;
    }

    private void Update()
    {
        age += Time.deltaTime;
        if (age >= Life)
        {
            Destroy(gameObject);
            return;
        }

        Velocity.y += Gravity * Time.deltaTime;
        Vector3 move = Velocity * Time.deltaTime;
        if (Wobble > 0f) move.x += Mathf.Sin(age * Wobble * 3f) * Time.deltaTime * Wobble;
        transform.position += move;
        transform.Rotate(0f, 0f, Spin * Time.deltaTime);

        // Pop in quickly, fade out at the end.
        float t = age / Life;
        float scale = Mathf.Min(1f, age / 0.08f);
        transform.localScale = baseScale * scale;
        float alpha = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
        sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * alpha);
    }
}
