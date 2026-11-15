using UnityEngine;

// The banner shown when the battle ends (SU-168), drawn with IMGUI by Battle while the slowed fighting plays on: "Victory"
// or "Defeat" on a dark panel across the upper middle of the screen, with a coloured strip along its top (blue for a
// victory, red for a defeat, the troop cards' Morale and Shock colours). It does not take clicks. Sizes are set for a
// 1080 px tall screen and scale with the screen height, like the troop cards (TroopCardHud).
public sealed class BattleEndBanner
{
    public const string VictoryText = "Victory";
    public const string DefeatText = "Defeat";
    public const int GuiDepth = -100;   // drawn over the battle HUD (lower GUI.depth is in front)

    const float ReferenceHeight = 1080f;
    const float MinScale = 0.8f;
    const float MaxScale = 2.5f;
    const float Width = 560f;
    const float Height = 130f;
    const float AccentHeight = 6f;
    const float TopFraction = 0.22f;    // the banner's centre, as a fraction of the screen height from the top
    const int FontSize = 72;

    static readonly Color PanelColor = new(0.07f, 0.07f, 0.09f, 0.88f);
    static readonly Color VictoryColor = new(0.22f, 0.48f, 1f);
    static readonly Color DefeatColor = new(0.9f, 0.18f, 0.18f);

    float _scale = -1f;
    GUIStyle _style;

    // The banner's text for the result.
    public static string TextFor(bool won) => won ? VictoryText : DefeatText;

    public void Draw(bool won)
    {
        if (Event.current.type != EventType.Repaint) return;
        EnsureStyle();
        float s = _scale;
        float width = Mathf.Min(Width * s, Screen.width);
        float height = Height * s;
        var rect = new Rect((Screen.width - width) * 0.5f, Screen.height * TopFraction - height * 0.5f, width, height);
        Fill(rect, PanelColor);
        Fill(new Rect(rect.x, rect.y, rect.width, AccentHeight * s), won ? VictoryColor : DefeatColor);
        GUI.Label(new Rect(rect.x, rect.y + AccentHeight * s, rect.width, rect.height - AccentHeight * s), TextFor(won), _style);
    }

    void EnsureStyle()
    {
        float scale = Mathf.Clamp(Screen.height / ReferenceHeight, MinScale, MaxScale);
        if (_style != null && Mathf.Approximately(scale, _scale)) return;
        _scale = scale;
        _style = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(FontSize * scale),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = false,
            clipping = TextClipping.Clip,
            richText = false,
            padding = new RectOffset(0, 0, 0, 0),
            margin = new RectOffset(0, 0, 0, 0)
        };
        _style.normal.textColor = Color.white;
    }

    static void Fill(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }
}
