using UnityEngine;

public enum BattleSpeedButton { Pause, Half, Play, Double }

// The battle speed bar, top-right corner, drawn with IMGUI by TroopPlayerOrders: Pause, ½ speed, Play (normal speed)
// and 2× speed. The button for the current state is lit; hovering a button shows its name under the bar. While paused
// the player can still move the camera, select troops and give orders; they carry them out when time runs again.
// Sizes are set for a 1080 px tall screen and scale with the screen height, like the troop cards (TroopCardHud).
public sealed class BattleSpeedHud
{
    public const float HalfSpeed = 0.5f;
    public const float NormalSpeed = 1f;
    public const float DoubleSpeed = 2f;

    const float ReferenceHeight = 1080f;
    const float MinScale = 0.8f;
    const float MaxScale = 2.5f;
    const float Margin = 12f;
    const float Padding = 6f;
    const float ButtonSize = 52f;
    const float Gap = 4f;
    const float IconFraction = 0.5f;      // icon size as a fraction of the button
    const float TooltipHeight = 30f;
    const int TriangleTextureSize = 64;

    static readonly Color PanelColor = new(0.07f, 0.07f, 0.09f, 0.88f);
    static readonly Color ButtonColor = new(1f, 1f, 1f, 0.07f);
    static readonly Color HoverColor = new(1f, 1f, 1f, 0.16f);
    static readonly Color ActiveColor = new(0.22f, 0.48f, 1f, 0.9f);
    static readonly Color PausedActiveColor = new(0.9f, 0.55f, 0.15f, 0.9f);
    static readonly Color IconColor = new(1f, 1f, 1f, 0.95f);
    static readonly Color IdleIconColor = new(1f, 1f, 1f, 0.7f);

    static readonly BattleSpeedButton[] Buttons =
        { BattleSpeedButton.Pause, BattleSpeedButton.Half, BattleSpeedButton.Play, BattleSpeedButton.Double };

    Rect _bounds;            // GUI-space rect of the bar drawn last, for Contains
    bool _visible;
    float _scale = -1f;
    GUIStyle _halfLabel, _tooltip;
    Texture2D _triangle;

    // Which button is lit for the game's current state.
    public static BattleSpeedButton ActiveButton(bool paused, float speed)
    {
        if (paused) return BattleSpeedButton.Pause;
        if (speed < NormalSpeed - 0.01f) return BattleSpeedButton.Half;
        if (speed > NormalSpeed + 0.01f) return BattleSpeedButton.Double;
        return BattleSpeedButton.Play;
    }

    // Game speed a speed button sets (Pause has none: it keeps the current speed).
    public static float SpeedOf(BattleSpeedButton button) => button switch
    {
        BattleSpeedButton.Half => HalfSpeed,
        BattleSpeedButton.Double => DoubleSpeed,
        _ => NormalSpeed,
    };

    public static string NameOf(BattleSpeedButton button) => button switch
    {
        BattleSpeedButton.Pause => "Pause (P)",
        BattleSpeedButton.Half => "Half speed",
        BattleSpeedButton.Double => "Double speed",
        _ => "Play",
    };

    // True when the screen position (origin bottom-left) is over the bar, so clicks on it are not orders.
    public bool Contains(Vector2 screenPosition) => _visible && _bounds.Contains(TroopScreenSpace.ToGui(screenPosition));

    public void Hide() => _visible = false;

    // Draws the bar with the active button lit. Returns true, with the button, when one was clicked during this event.
    public bool Draw(BattleSpeedButton active, out BattleSpeedButton clicked)
    {
        clicked = active;
        EnsureResources();
        float s = _scale;
        float size = ButtonSize * s;
        float gap = Gap * s;
        float pad = Padding * s;
        float width = 2f * pad + Buttons.Length * size + (Buttons.Length - 1) * gap;
        float height = 2f * pad + size;
        _bounds = new Rect(Screen.width - Margin * s - width, Margin * s, width, height);
        _visible = true;

        Event current = Event.current;
        Fill(_bounds, PanelColor);
        bool wasClicked = false;
        BattleSpeedButton? hovered = null;
        for (int i = 0; i < Buttons.Length; i++)
        {
            BattleSpeedButton button = Buttons[i];
            var rect = new Rect(_bounds.x + pad + i * (size + gap), _bounds.y + pad, size, size);
            bool over = rect.Contains(current.mousePosition);
            bool lit = button == active;
            if (over) hovered = button;

            Color background = lit ? (button == BattleSpeedButton.Pause ? PausedActiveColor : ActiveColor)
                : over ? HoverColor : ButtonColor;
            Fill(rect, background);
            DrawIcon(rect, button, lit || over ? IconColor : IdleIconColor);

            if (current.type == EventType.MouseDown && current.button == 0 && over)
            {
                clicked = button;
                wasClicked = true;
                current.Use();
            }
        }

        if (hovered.HasValue)
        {
            var tip = new Rect(_bounds.x, _bounds.yMax + gap, _bounds.width, TooltipHeight * s);
            Fill(tip, PanelColor);
            GUI.Label(tip, NameOf(hovered.Value), _tooltip);
        }
        return wasClicked;
    }

    public void Destroy()
    {
        if (_triangle != null) Object.Destroy(_triangle);
        _triangle = null;
    }

    // --- Icons ---

    void DrawIcon(Rect button, BattleSpeedButton kind, Color color)
    {
        float icon = button.width * IconFraction;
        var area = new Rect(button.center.x - icon * 0.5f, button.center.y - icon * 0.5f, icon, icon);
        switch (kind)
        {
            case BattleSpeedButton.Pause:
            {
                float bar = icon * 0.3f;
                Fill(new Rect(area.x + icon * 0.1f, area.y, bar, icon), color);
                Fill(new Rect(area.xMax - icon * 0.1f - bar, area.y, bar, icon), color);
                break;
            }
            case BattleSpeedButton.Play:
                Triangle(area, color);
                break;
            case BattleSpeedButton.Double:
            {
                float w = icon * 0.62f;
                Triangle(new Rect(area.x - icon * 0.08f, area.y, w, icon), color);
                Triangle(new Rect(area.xMax + icon * 0.08f - w, area.y, w, icon), color);
                break;
            }
            case BattleSpeedButton.Half:
            {
                // A smaller play triangle with "½" beside it.
                float t = icon * 0.7f;
                Triangle(new Rect(area.x - icon * 0.12f, area.center.y - t * 0.5f, t, t), color);
                Color previous = GUI.contentColor;
                GUI.contentColor = color;
                GUI.Label(new Rect(area.center.x, area.y, area.width * 0.6f, area.height), "½", _halfLabel);
                GUI.contentColor = previous;
                break;
            }
        }
    }

    void Triangle(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, _triangle, ScaleMode.StretchToFill, true);
        GUI.color = previous;
    }

    // --- Resources ---

    void EnsureResources()
    {
        if (_triangle == null) _triangle = CreateTriangle(TriangleTextureSize);
        float scale = Mathf.Clamp(Screen.height / ReferenceHeight, MinScale, MaxScale);
        if (_tooltip != null && Mathf.Approximately(scale, _scale)) return;
        _scale = scale;
        _halfLabel = Style(26, FontStyle.Bold, TextAnchor.MiddleLeft);
        _tooltip = Style(18, FontStyle.Normal, TextAnchor.MiddleCenter);
    }

    GUIStyle Style(float size, FontStyle fontStyle, TextAnchor anchor)
    {
        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(size * _scale),
            fontStyle = fontStyle,
            alignment = anchor,
            wordWrap = false,
            clipping = TextClipping.Overflow,
            richText = false,
            padding = new RectOffset(0, 0, 0, 0),
            margin = new RectOffset(0, 0, 0, 0)
        };
        style.normal.textColor = Color.white;
        return style;
    }

    // White right-pointing triangle with anti-aliased edges, filling the texture.
    static Texture2D CreateTriangle(int size)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "Battle Speed Triangle",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        var a = new Vector2(0f, 0f);
        var b = new Vector2(size, size * 0.5f);
        var c = new Vector2(0f, size);
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            var p = new Vector2(x + 0.5f, y + 0.5f);
            // Distance inside each edge (positive inside); the smallest decides the coverage.
            float inside = Mathf.Min(EdgeDistance(a, b, p), Mathf.Min(EdgeDistance(b, c, p), EdgeDistance(c, a, p)));
            byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(inside + 0.5f) * 255f);
            pixels[y * size + x] = new Color32(255, 255, 255, alpha);
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return texture;
    }

    // Signed distance from p to the edge from -> to; positive on the left of the edge (inside a counter-clockwise
    // triangle).
    static float EdgeDistance(Vector2 from, Vector2 to, Vector2 p)
    {
        Vector2 edge = to - from;
        return (edge.x * (p.y - from.y) - edge.y * (p.x - from.x)) / edge.magnitude;
    }

    static void Fill(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }
}
