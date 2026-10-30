using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Loads art, sounds and the font from the Resources folder and keeps them
/// in memory so each file is only loaded once.
///
/// Folder layout (inside Assets/Resources):
///   Art/   PNG images
///   Audio/ WAV sound effects
///   Fonts/ TTF font
/// </summary>
public static class GameAssets
{
    private static readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
    private static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
    private static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    private static Font font;

    /// <summary>The main game font (Fredoka). Falls back to Unity's built-in font.</summary>
    public static Font MainFont
    {
        get
        {
            if (font == null) font = Resources.Load<Font>("Fonts/Fredoka-700");
            if (font == null) font = LoadBuiltinFont();
            return font;
        }
    }

    /// <summary>Loads an image from Resources/Art as a texture (used by the UI).</summary>
    public static Texture2D Texture(string name)
    {
        Texture2D tex;
        if (textures.TryGetValue(name, out tex)) return tex;

        tex = Resources.Load<Texture2D>("Art/" + name);
        if (tex == null)
        {
            Debug.LogWarning("Missing art: Resources/Art/" + name + ".png");
            tex = Texture2D.whiteTexture;
        }
        textures[name] = tex;
        return tex;
    }

    /// <summary>
    /// Makes a sprite from an image. The pivot is a point from 0 to 1
    /// (0.5, 0.5 is the center). Width in world units is set by pixelsPerUnit.
    /// The sprite is created in code, so the import settings of the PNG do not matter.
    /// </summary>
    public static Sprite Sprite(string name, float pixelsPerUnit, Vector2 pivot)
    {
        string key = name + "|" + pixelsPerUnit + "|" + pivot;
        Sprite sprite;
        if (sprites.TryGetValue(key, out sprite)) return sprite;

        Texture2D tex = Texture(name);
        sprite = UnityEngine.Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, pixelsPerUnit);
        sprites[key] = sprite;
        return sprite;
    }

    /// <summary>Same as above with the pivot in the center.</summary>
    public static Sprite Sprite(string name, float pixelsPerUnit)
    {
        return Sprite(name, pixelsPerUnit, new Vector2(0.5f, 0.5f));
    }

    /// <summary>
    /// How many pixels from each edge are "corner" in a 9-sliced image.
    /// A quarter of the smaller side, up to 32 pixels, works for most panels and buttons.
    /// </summary>
    public static int SliceBorder(Texture2D tex)
    {
        return Mathf.Clamp(Mathf.Min(tex.width, tex.height) / 4, 2, 32);
    }

    /// <summary>Makes a 9-sliced sprite (corners keep their size when stretched).</summary>
    public static Sprite SlicedSprite(string name, float pixelsPerUnit)
    {
        string key = name + "|sliced|" + pixelsPerUnit;
        Sprite sprite;
        if (sprites.TryGetValue(key, out sprite)) return sprite;

        Texture2D tex = Texture(name);
        float border = SliceBorder(tex);
        sprite = UnityEngine.Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f),
            pixelsPerUnit, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        sprites[key] = sprite;
        return sprite;
    }

    /// <summary>
    /// The sprite of an image as Unity imported it (100 pixels per unit, pivot in
    /// the center). Board objects use this, because only imported sprites can be
    /// saved in a scene. If the image was not imported as a sprite, a sprite is
    /// made in code instead (this works while playing, but cannot be saved).
    /// </summary>
    public static Sprite SceneSprite(string name)
    {
        string key = name + "|scene";
        Sprite sprite;
        if (sprites.TryGetValue(key, out sprite) && sprite != null) return sprite;

        sprite = Resources.Load<Sprite>("Art/" + name);
        if (sprite == null) sprite = Sprite(name, 100f);
        sprites[key] = sprite;
        return sprite;
    }

    /// <summary>A 9-sliced version of SceneSprite (the border comes from the import settings).</summary>
    public static Sprite SceneSlicedSprite(string name)
    {
        Sprite sprite = Resources.Load<Sprite>("Art/" + name);
        if (sprite != null && sprite.border != Vector4.zero) return sprite;
        return SlicedSprite(name, 100f);
    }

    /// <summary>Forgets everything loaded, so new or changed images are picked up (used by the editor button).</summary>
    public static void ClearCache()
    {
        textures.Clear();
        sprites.Clear();
        clips.Clear();
        font = null;
        unlitSpriteMaterial = null;
        searchedForUnlit = false;
    }

    private static Material unlitSpriteMaterial;
    private static bool searchedForUnlit;

    /// <summary>
    /// Adds a sprite renderer to an object. In URP 2D projects, sprites are lit by
    /// 2D lights and turn black if a scene has no light, so this uses an unlit
    /// sprite material: Resources/Materials/SpriteUnlit (made by the editor button),
    /// or one made in code while playing. In the Built-in pipeline nothing changes.
    /// </summary>
    public static SpriteRenderer AddSpriteRenderer(GameObject go)
    {
        var sr = go.AddComponent<SpriteRenderer>();
        if (!searchedForUnlit)
        {
            searchedForUnlit = true;
            unlitSpriteMaterial = Resources.Load<Material>("Materials/SpriteUnlit");
            if (unlitSpriteMaterial == null && Application.isPlaying)
            {
                Shader unlit = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (unlit != null) unlitSpriteMaterial = new Material(unlit);
            }
        }
        if (unlitSpriteMaterial != null) sr.sharedMaterial = unlitSpriteMaterial;
        return sr;
    }

    /// <summary>Loads a sound from Resources/Audio.</summary>
    public static AudioClip Clip(string name)
    {
        AudioClip clip;
        if (clips.TryGetValue(name, out clip)) return clip;

        clip = Resources.Load<AudioClip>("Audio/" + name);
        if (clip == null) Debug.LogWarning("Missing sound: Resources/Audio/" + name + ".wav");
        clips[name] = clip;
        return clip;
    }

    private static Font LoadBuiltinFont()
    {
        // The built-in font changed its name in newer Unity versions, so try both.
        try { return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
        try { return Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { }
        return null;
    }
}
