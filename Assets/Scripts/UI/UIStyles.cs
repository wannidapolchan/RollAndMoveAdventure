using UnityEngine;

/// <summary>
/// All UI drawing helpers in one place: text, buttons, panels and images.
/// The UI is designed for a 1920 x 1080 screen. Every size is multiplied by
/// Scale so it looks the same (and stays sharp) on any screen size.
/// </summary>
public class UIStyles
{
    public enum ButtonColor { Orange, Blue }

    public static readonly Color Gold = new Color(1f, 0.82f, 0.3f);
    public static readonly Color SoftWhite = new Color(0.95f, 0.95f, 0.97f);
    public static readonly Color Muted = new Color(0.7f, 0.76f, 0.8f);
    public static readonly Color DarkText = new Color(0.25f, 0.18f, 0.14f);
    public static readonly Color Good = new Color(0.25f, 0.65f, 0.35f);
    public static readonly Color Bad = new Color(0.85f, 0.3f, 0.27f);

    public float Scale { get; private set; }
    /// <summary>Screen width in design pixels (1920 on a 16:9 screen).</summary>
    public float Width { get; private set; }

    private GUIStyle text;
    private GUIStyle orangeButton;
    private GUIStyle blueButton;
    private GUIStyle grayButton;
    private GUIStyle darkPanel;
    private GUIStyle lightPanel;
    private GUIStyle invisibleButton;
    private SoundManager sound;

    public UIStyles(SoundManager soundManager)
    {
        sound = soundManager;
    }

    /// <summary>Creates the styles once and updates the scale each frame.</summary>
    public void Begin()
    {
        Scale = Screen.height / 1080f;
        Width = Screen.width / Scale;
        if (text != null) return;

        Font font = GameAssets.MainFont;
        text = new GUIStyle();
        text.font = font;

        orangeButton = MakeButton(font, "button", "button_hover", "button_down");
        blueButton = MakeButton(font, "button_blue", "button_blue_hover", "button_blue");
        grayButton = MakeButton(font, "button_off", "button_off", "button_off");
        grayButton.normal.textColor = new Color(0.85f, 0.85f, 0.88f);

        darkPanel = MakePanel("panel_dark");
        lightPanel = MakePanel("panel_light");
        invisibleButton = new GUIStyle();
    }

    private static GUIStyle MakeButton(Font font, string normal, string hover, string down)
    {
        var style = new GUIStyle();
        style.font = font;
        style.alignment = TextAnchor.MiddleCenter;
        int b = GameAssets.SliceBorder(GameAssets.Texture(normal));
        style.border = new RectOffset(b, b, b, b);
        style.normal.background = GameAssets.Texture(normal);
        style.hover.background = GameAssets.Texture(hover);
        style.active.background = GameAssets.Texture(down);
        style.normal.textColor = Color.white;
        style.hover.textColor = Color.white;
        style.active.textColor = Color.white;
        return style;
    }

    private static GUIStyle MakePanel(string image)
    {
        var style = new GUIStyle();
        int b = GameAssets.SliceBorder(GameAssets.Texture(image));
        style.border = new RectOffset(b, b, b, b);
        style.normal.background = GameAssets.Texture(image);
        return style;
    }

    // ---------------------------------------------------------------- layout

    /// <summary>Converts a rectangle in design pixels to real screen pixels.</summary>
    public Rect R(float x, float y, float w, float h)
    {
        return new Rect(x * Scale, y * Scale, w * Scale, h * Scale);
    }

    private int Size(float designSize)
    {
        return Mathf.Max(1, Mathf.RoundToInt(designSize * Scale));
    }

    // ---------------------------------------------------------------- drawing

    public void Label(Rect r, string content, float size, Color color,
                      TextAnchor align = TextAnchor.MiddleCenter, bool wrap = false)
    {
        text.fontSize = Size(size);
        text.normal.textColor = color;
        text.alignment = align;
        text.wordWrap = wrap;
        GUI.Label(r, content, text);
    }

    /// <summary>Text with a dark drop shadow, for big titles.</summary>
    public void ShadowLabel(Rect r, string content, float size, Color color,
                            TextAnchor align = TextAnchor.MiddleCenter, bool wrap = false)
    {
        float offset = Mathf.Max(2f, size * 0.06f) * Scale;
        Rect shadowRect = new Rect(r.x + offset, r.y + offset, r.width, r.height);
        Label(shadowRect, content, size, new Color(0f, 0f, 0f, 0.45f * color.a), align, wrap);
        Label(r, content, size, color, align, wrap);
    }

    /// <summary>A button that plays a click. Disabled buttons are gray and cannot be clicked.</summary>
    public bool Button(Rect r, string content, float size, ButtonColor color, bool enabled = true)
    {
        GUIStyle style = !enabled ? grayButton : (color == ButtonColor.Orange ? orangeButton : blueButton);
        style.fontSize = Size(size);
        style.padding = new RectOffset(4, 4, 0, Size(8)); // keep text above the button's bottom edge

        if (!enabled)
        {
            GUI.Box(r, content, style);
            return false;
        }
        bool clicked = GUI.Button(r, content, style);
        if (clicked) sound.Play("click");
        return clicked;
    }

    /// <summary>A clickable area with no look of its own (for custom drawn buttons).</summary>
    public bool ClickArea(Rect r)
    {
        return GUI.Button(r, GUIContent.none, invisibleButton);
    }

    public void DarkPanel(Rect r) { GUI.Box(r, GUIContent.none, darkPanel); }
    public void LightPanel(Rect r) { GUI.Box(r, GUIContent.none, lightPanel); }

    public void Image(Rect r, string image, float alpha = 1f)
    {
        Color old = GUI.color;
        GUI.color = new Color(old.r, old.g, old.b, old.a * alpha);
        GUI.DrawTexture(r, GameAssets.Texture(image), ScaleMode.ScaleToFit);
        GUI.color = old;
    }

    /// <summary>Fills a rectangle with a flat color (used to dim the screen).</summary>
    public void Fill(Rect r, Color color)
    {
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = old;
    }
}
