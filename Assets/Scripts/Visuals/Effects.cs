using UnityEngine;

/// <summary>
/// Small visual effects: sparkles, flying coins, confetti, floating text and
/// camera shake. Every effect cleans itself up when it is done.
/// </summary>
public static class Effects
{
    /// <summary>Shoots small sprites out from a point.</summary>
    public static void Burst(Vector3 position, string image, Color color, int count,
                             float speed, float size, float life, float gravity)
    {
        for (int i = 0; i < count; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float power = speed * Random.Range(0.5f, 1f);
            Vector3 velocity = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * power;
            float particleSize = size * Random.Range(0.6f, 1.2f);
            EffectParticle.Spawn(position, image, color, velocity, gravity, particleSize, life * Random.Range(0.7f, 1.1f));
        }
    }

    /// <summary>Golden sparkles, used for ladders and escapes.</summary>
    public static void Sparkles(Vector3 position)
    {
        Burst(position, "sparkle", new Color(1f, 0.92f, 0.5f), 16, 3.5f, 0.35f, 0.8f, 0f);
    }

    /// <summary>Coins jump up and fall, used when a player gets coins.</summary>
    public static void CoinShower(Vector3 position, int coins)
    {
        for (int i = 0; i < 4 + coins * 3; i++)
        {
            Vector3 velocity = new Vector3(Random.Range(-1.8f, 1.8f), Random.Range(4f, 6.5f), 0f);
            EffectParticle.Spawn(position, "coin", Color.white, velocity, -14f, 0.35f, 0.9f);
        }
    }

    /// <summary>Colorful confetti falling over the whole view (win screen).</summary>
    public static void Confetti(Camera cam)
    {
        Color[] colors =
        {
            new Color(1f, 0.4f, 0.4f), new Color(1f, 0.85f, 0.3f), new Color(0.4f, 0.85f, 0.5f),
            new Color(0.4f, 0.65f, 1f), new Color(0.8f, 0.5f, 1f)
        };
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;
        Vector3 center = cam.transform.position;

        for (int i = 0; i < 90; i++)
        {
            Vector3 pos = new Vector3(center.x + Random.Range(-halfWidth, halfWidth),
                                      center.y + halfHeight + Random.Range(0.2f, 3f), 0f);
            Vector3 velocity = new Vector3(Random.Range(-1f, 1f), Random.Range(-3.5f, -1.5f), 0f);
            string image = Random.value < 0.75f ? "square" : "sparkle";
            EffectParticle p = EffectParticle.Spawn(pos, image, colors[Random.Range(0, colors.Length)],
                                                    velocity, -1.5f, Random.Range(0.12f, 0.22f), 4.5f);
            p.Spin = Random.Range(-400f, 400f);
            p.Wobble = Random.Range(1f, 3f);
        }
    }

    /// <summary>Text that pops up, floats upward and fades, like "+2 coins".</summary>
    public static void FloatingText(Vector3 position, string text, Color color)
    {
        global::FloatingText.Spawn(position, text, color);
    }

    /// <summary>Shakes the camera a little.</summary>
    public static void Shake(float strength, float duration)
    {
        CameraRig rig = Camera.main != null ? Camera.main.GetComponent<CameraRig>() : null;
        if (rig != null) rig.Shake(strength, duration);
    }
}
