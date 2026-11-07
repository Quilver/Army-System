using System.Collections.Generic;
using UnityEngine;

// The combat log panel (left edge, drawn with IMGUI by TroopPlayerOrders). An arrow tab on the left edge of the screen
// opens it; the arrow on the panel's edge hides it again. It lists every clash and ranged attack of the battle
// (CombatLog), newest at the bottom: who fought, each side's Combat Power with its skill, charge and support parts, the
// chance and the roll, and the damage (raw, the armour roll, the HP lost). Toggles show or hide clashes and shots. It
// follows the newest entry while scrolled to the bottom. Entries wrap to the panel's width. Sizes are set for a 1080 px
// tall screen and scale with it.
public sealed class CombatLogHud
{
    const float ReferenceHeight = 1080f;
    const float MinScale = 0.8f;
    const float MaxScale = 2.5f;
    const float Margin = 12f;
    const float TopMargin = 12f;
    const float Width = 620f;
    const float HeightFraction = 0.5f;    // of the screen height
    const float TabWidth = 30f;
    const float TabHeight = 84f;
    const float HeaderHeight = 34f;
    const float Padding = 8f;
    const float EntryGap = 6f;
    const float EntryPadding = 3f;
    const float ColourBar = 4f;
    const int FontSize = 14;

    static readonly Color PanelColor = new(0.07f, 0.07f, 0.09f, 0.9f);
    static readonly Color TabColor = new(0.07f, 0.07f, 0.09f, 0.88f);
    static readonly Color TabHoverColor = new(0.16f, 0.16f, 0.2f, 0.95f);
    static readonly Color EntryColor = new(1f, 1f, 1f, 0.04f);
    static readonly Color ToggleOnColor = new(0.22f, 0.48f, 1f, 0.9f);
    static readonly Color ToggleOffColor = new(1f, 1f, 1f, 0.08f);
    static readonly Color DimText = new(1f, 1f, 1f, 0.6f);

    sealed class Row
    {
        public string Text;
        public float Height = -1f;   // wrapped text height at _layoutWidth; -1 until measured
        public Color Colour;
        public CombatLogKind Kind;
    }

    readonly List<Row> _rows = new();
    readonly List<int> _visible = new();
    int _seen;
    bool _open;
    bool _showClashes = true, _showShots = true;
    bool _followNewest = true;
    Vector2 _scroll;
    Rect _panel, _tab;
    bool _drawn;
    float _scale = -1f;
    float _layoutWidth = -1f;
    readonly GUIContent _content = new();
    GUIStyle _text, _header, _arrow, _toggle;

    public bool IsOpen => _open;
    public void SetOpen(bool open) => _open = open;

    // True when the screen position (origin bottom-left) is over the panel or its tab, so clicks there are not orders.
    public bool Contains(Vector2 screenPosition)
    {
        if (!_drawn) return false;
        Vector2 gui = TroopScreenSpace.ToGui(screenPosition);
        return _tab.Contains(gui) || (_open && _panel.Contains(gui));
    }

    public void Hide() => _drawn = false;

    public void Draw()
    {
        EnsureStyles();
        Sync();
        float s = _scale;
        float height = Mathf.Max(200f * s, Screen.height * HeightFraction);
        _panel = new Rect(Margin * s, TopMargin * s, Mathf.Min(Width * s, Screen.width * 0.6f), height);
        float tabX = _open ? _panel.xMax : 0f;
        _tab = new Rect(tabX, _panel.y + (HeaderHeight * s - Mathf.Min(HeaderHeight * s, TabHeight * s)) * 0.5f,
            TabWidth * s, TabHeight * s);
        _drawn = true;

        Event current = Event.current;
        bool overTab = _tab.Contains(current.mousePosition);
        Fill(_tab, overTab ? TabHoverColor : TabColor);
        GUI.Label(new Rect(_tab.x, _tab.y, _tab.width, _tab.height * 0.55f), _open ? "◀" : "▶", _arrow);
        GUI.Label(new Rect(_tab.x, _tab.y + _tab.height * 0.45f, _tab.width, _tab.height * 0.5f), "Log", _toggle);
        if (current.type == EventType.MouseDown && current.button == 0 && overTab)
        {
            _open = !_open;
            current.Use();
        }
        if (_open) DrawPanel(s);
    }

    void DrawPanel(float s)
    {
        Fill(_panel, PanelColor);
        float pad = Padding * s;
        var header = new Rect(_panel.x + pad, _panel.y, _panel.width - 2f * pad, HeaderHeight * s);
        GUI.Label(header, "Combat log", _header);

        float toggleWidth = 84f * s, toggleHeight = 22f * s, gap = 4f * s;
        var shots = new Rect(header.xMax - toggleWidth, header.y + (header.height - toggleHeight) * 0.5f, toggleWidth, toggleHeight);
        var clashes = new Rect(shots.x - gap - toggleWidth, shots.y, toggleWidth, toggleHeight);
        _showClashes = Toggle(clashes, "Clashes", _showClashes);
        _showShots = Toggle(shots, "Shots", _showShots);

        var view = new Rect(_panel.x + pad, header.yMax, _panel.width - 2f * pad, _panel.height - header.height - pad);
        float innerWidth = view.width - 16f * s;
        float textInset = ColourBar * s + 5f * s;
        float textWidth = innerWidth - textInset - 4f * s;
        Measure(textWidth);

        _visible.Clear();
        float entryGap = EntryGap * s, entryPad = EntryPadding * s;
        float contentHeight = 0f;
        for (int i = 0; i < _rows.Count; i++)
        {
            Row row = _rows[i];
            if (row.Kind == CombatLogKind.Clash ? !_showClashes : !_showShots) continue;
            _visible.Add(i);
            contentHeight += row.Height + 2f * entryPad + entryGap;
        }

        var content = new Rect(0f, 0f, innerWidth, Mathf.Max(contentHeight, view.height));
        float maxScroll = Mathf.Max(0f, content.height - view.height);
        if (_followNewest) _scroll.y = maxScroll;

        if (_visible.Count == 0)
        {
            GUI.Label(new Rect(view.x, view.y, view.width, 40f * s), "No clashes or shots yet.", _text);
            return;
        }

        Vector2 before = _scroll;
        _scroll = GUI.BeginScrollView(view, _scroll, content, false, true);
        float y = 0f;
        foreach (int index in _visible)
        {
            Row row = _rows[index];
            float rowHeight = row.Height + 2f * entryPad;
            if (y + rowHeight >= _scroll.y && y <= _scroll.y + view.height)
            {
                var rect = new Rect(0f, y, innerWidth, rowHeight);
                Fill(rect, EntryColor);
                Fill(new Rect(rect.x, rect.y, ColourBar * s, rect.height), row.Colour);
                GUI.Label(new Rect(rect.x + textInset, rect.y + entryPad, textWidth, row.Height), row.Text, _text);
            }
            y += rowHeight + entryGap;
        }
        GUI.EndScrollView();
        // Scrolling up stops following the newest entry; scrolling back to the bottom follows it again.
        if (!Mathf.Approximately(before.y, _scroll.y)) _followNewest = _scroll.y >= maxScroll - 1f;
    }

    // Wrapped height of each entry at this width. Re-measured when the width (or the text size) changes.
    void Measure(float width)
    {
        bool remeasure = !Mathf.Approximately(width, _layoutWidth);
        _layoutWidth = width;
        foreach (var row in _rows)
        {
            if (!remeasure && row.Height >= 0f) continue;
            _content.text = row.Text;
            row.Height = _text.CalcHeight(_content, width);
        }
    }

    bool Toggle(Rect rect, string label, bool on)
    {
        Fill(rect, on ? ToggleOnColor : ToggleOffColor);
        Color previous = GUI.contentColor;
        GUI.contentColor = on ? Color.white : DimText;
        GUI.Label(rect, label, _toggle);
        GUI.contentColor = previous;
        Event current = Event.current;
        if (current.type == EventType.MouseDown && current.button == 0 && rect.Contains(current.mousePosition))
        {
            current.Use();
            _followNewest = true;
            return !on;
        }
        return on;
    }

    // Copies new entries from the battle's CombatLog, formatted once. A new battle (fewer entries recorded than seen)
    // starts the list again.
    void Sync()
    {
        int total = CombatLog.TotalRecorded;
        if (total < _seen)
        {
            _rows.Clear();
            _seen = 0;
        }
        int fresh = Mathf.Min(total - _seen, CombatLog.Count);
        for (int i = CombatLog.Count - fresh; i < CombatLog.Count; i++)
        {
            CombatLogEntry entry = CombatLog.Get(i);
            _rows.Add(new Row { Text = CombatLog.Format(entry), Colour = entry.Attacker.Colour, Kind = entry.Kind });
        }
        _seen = total;
        int excess = _rows.Count - CombatLog.Capacity;
        if (excess > 0) _rows.RemoveRange(0, excess);
    }

    void EnsureStyles()
    {
        float scale = Mathf.Clamp(Screen.height / ReferenceHeight, MinScale, MaxScale);
        if (_text != null && Mathf.Approximately(scale, _scale)) return;
        _scale = scale;
        _layoutWidth = -1f;
        _text = Style(FontSize, FontStyle.Normal, TextAnchor.UpperLeft);
        _text.wordWrap = true;
        _header = Style(18, FontStyle.Bold, TextAnchor.MiddleLeft);
        _arrow = Style(18, FontStyle.Bold, TextAnchor.MiddleCenter);
        _toggle = Style(13, FontStyle.Normal, TextAnchor.MiddleCenter);
    }

    GUIStyle Style(float size, FontStyle fontStyle, TextAnchor anchor)
    {
        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(size * _scale),
            fontStyle = fontStyle,
            alignment = anchor,
            wordWrap = false,
            clipping = TextClipping.Clip,
            richText = false,
            padding = new RectOffset(0, 0, 0, 0),
            margin = new RectOffset(0, 0, 0, 0)
        };
        style.normal.textColor = Color.white;
        return style;
    }

    static void Fill(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }
}
