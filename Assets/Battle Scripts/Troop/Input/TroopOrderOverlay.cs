using UnityEngine;

// IMGUI drawing for the player order controls (spec §6): help panel, selection box, ghost footprints, facing arrows,
// shared joints and the ghost tether. All positions are GUI space (origin top-left) unless noted.
public static class TroopOrderOverlay
{
    const float DotSize = 12f;
    const float DashLength = 8f;
    static readonly Color TetherInside = new(0.35f, 1f, 0.45f, 0.95f);
    static readonly Color TetherOutside = new(1f, 0.25f, 0.2f, 0.95f);
    static readonly Color JointColor = new(1f, 1f, 1f, 0.95f);
    // Help panel: top centre of the screen, HelpScale x the original 480 px panel and 12 px text. It is kept clear of
    // the troop status panel on the right (TroopDebugOverlay, 25% of the screen width) and wraps to fit.
    const float HelpScale = 3f;
    const float HelpBaseWidth = 480f;
    const float HelpBaseFontSize = 12f;
    const float HelpMargin = 10f;
    const float HelpPadding = 12f;
    const float RightPanelWidthFraction = 0.25f;
    static Texture2D _dotTexture;
    static GUIStyle _helpStyle;
    static Rect _helpRect;

    // Where the help panel was last drawn (GUI space). Clicks inside it do not select or issue orders.
    public static Rect HelpRect => _helpRect;

    public static void DrawHelp(int selectedCount)
    {
        _helpStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(HelpBaseFontSize * HelpScale),
            wordWrap = true,
            richText = false
        };
        string text =
            "LMB click / drag: select (Shift toggles)   Alt + order: unlink / keep reserves unlinked\n" +
            "RMB: enemy = pursue, ground = move, drag = battle line   Selected: " + selectedCount + "\n" +
            "Tether: green = stays chained, dashed red = link breaks on release";

        // Centred, and no wider than the gap between the screen's left edge and the right-hand status panel allows.
        float rightPanelLeft = Screen.width * (1f - RightPanelWidthFraction) - HelpMargin;
        float maxWidth = 2f * (rightPanelLeft - HelpMargin - Screen.width * 0.5f);
        float width = Mathf.Max(200f, Mathf.Min(HelpBaseWidth * HelpScale, maxWidth));
        float textHeight = _helpStyle.CalcHeight(new GUIContent(text), width - 2f * HelpPadding);
        _helpRect = new Rect((Screen.width - width) * 0.5f, HelpMargin, width, textHeight + 2f * HelpPadding);

        GUI.Box(_helpRect, GUIContent.none);
        GUI.Label(new Rect(_helpRect.x + HelpPadding, _helpRect.y + HelpPadding, width - 2f * HelpPadding, textHeight),
            text, _helpStyle);
    }

    // Start and end are screen positions (origin bottom-left), as read from the mouse.
    public static void DrawSelectionBox(Vector2 start, Vector2 end)
    {
        Rect rect = Rect.MinMaxRect(
            Mathf.Min(start.x, end.x),
            Screen.height - Mathf.Max(start.y, end.y),
            Mathf.Max(start.x, end.x),
            Screen.height - Mathf.Min(start.y, end.y));
        Color previous = GUI.color;
        GUI.color = new Color(0.25f, 0.8f, 1f, 0.16f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = new Color(0.45f, 0.88f, 1f, 0.9f);
        const float border = 2f;
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, rect.width, border), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMax - border, rect.width, border), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, border, rect.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMax - border, rect.yMin, border, rect.height), Texture2D.whiteTexture);
        GUI.color = previous;
    }

    public static void DrawPlainLine(Vector2 guiStart, Vector2 guiEnd) => DrawGuiLine(guiStart, guiEnd, Color.white, 1f);

    // The projected footprint of a troop: four sides, the front face brighter.
    public static void DrawFootprint(Camera camera, in TroopFootprint footprint, Color army)
    {
        for (int i = 0; i < 4; i++)
        {
            Vector2 a = TroopScreenSpace.WorldToGui(camera, footprint.OuterCorner(i));
            Vector2 b = TroopScreenSpace.WorldToGui(camera, footprint.OuterCorner(i + 1));
            DrawGuiLine(a, b, i == 0 ? army : new Color(army.r, army.g, army.b, 0.55f), i == 0 ? 3f : 2f);
        }
    }

    public static void DrawFacing(Camera camera, Vector2 worldCenter, Vector2 facing, float worldLength)
    {
        Vector2 a = TroopScreenSpace.WorldToGui(camera, worldCenter);
        Vector2 tip = worldCenter + facing.normalized * worldLength;
        Vector2 b = TroopScreenSpace.WorldToGui(camera, tip);
        DrawGuiLine(a, b, Color.white, 2f);
        Vector2 back = (a - b).sqrMagnitude > 1f ? (a - b).normalized * 8f : Vector2.zero;
        Vector2 side = new(-back.y, back.x);
        DrawGuiLine(b, b + back + side * 0.6f, Color.white, 2f);
        DrawGuiLine(b, b + back - side * 0.6f, Color.white, 2f);
    }

    public static void DrawJoint(Vector2 gui) => DrawDot(gui, JointColor);

    public static void DrawMarker(Vector2 gui, Color color) => DrawDot(gui, color);

    // Solid green while the link stays (inside the envelope), dashed red when it will break (outside, or Alt).
    public static void DrawTether(Vector2 guiFrom, Vector2 guiTo, bool staysLinked)
    {
        if (staysLinked) DrawGuiLine(guiFrom, guiTo, TetherInside, 2f);
        else DrawDashedLine(guiFrom, guiTo, TetherOutside, 2f);
    }

    static void DrawDot(Vector2 gui, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(new Rect(gui.x - DotSize * 0.5f, gui.y - DotSize * 0.5f, DotSize, DotSize), DotTexture());
        GUI.color = previous;
    }

    static void DrawDashedLine(Vector2 guiStart, Vector2 guiEnd, Color color, float thickness)
    {
        Vector2 delta = guiEnd - guiStart;
        float length = delta.magnitude;
        if (length < 1f) return;
        Vector2 direction = delta / length;
        for (float t = 0f; t < length; t += DashLength * 2f)
            DrawGuiLine(guiStart + direction * t, guiStart + direction * Mathf.Min(length, t + DashLength), color, thickness);
    }

    static Texture2D DotTexture()
    {
        if (_dotTexture != null) return _dotTexture;
        const int size = 32;
        _dotTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.HideAndDontSave,
            filterMode = FilterMode.Bilinear
        };
        float radius = size * 0.5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radius, radius));
            _dotTexture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(radius - distance)));
        }
        _dotTexture.Apply();
        return _dotTexture;
    }

    static void DrawGuiLine(Vector2 guiStart, Vector2 guiEnd, Color color, float thickness)
    {
        Vector2 delta = guiEnd - guiStart;
        if (delta.sqrMagnitude < 1f) return;

        Matrix4x4 previousMatrix = GUI.matrix;
        Color previousColor = GUI.color;
        GUI.color = color;
        GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, guiStart);
        GUI.DrawTexture(new Rect(guiStart.x, guiStart.y - thickness * 0.5f, delta.magnitude, thickness), Texture2D.whiteTexture);
        GUI.matrix = previousMatrix;
        GUI.color = previousColor;
    }
}
