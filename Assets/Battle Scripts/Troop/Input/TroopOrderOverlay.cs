using System.Collections.Generic;
using UnityEngine;

// IMGUI drawing for the player order controls: help panel, selection box and order-line preview.
// All positions passed in are GUI space (origin top-left) unless noted.
public static class TroopOrderOverlay
{
    const float DotSize = 12f;
    static readonly Color ClampedLineColor = new(1f, 0.65f, 0.15f, 0.95f);
    static readonly Color LineColor = new(0.45f, 0.88f, 1f, 0.95f);
    static Texture2D _dotTexture;

    public static void DrawHelp(int selectedCount)
    {
        GUILayout.BeginArea(new Rect(16f, Screen.height - 86f, 480f, 70f), GUI.skin.box);
        GUILayout.Label("Left-click / drag: select friendly troops (Shift adds)");
        GUILayout.Label("Right-click / drag: move selection / set battle line   Selected: " + selectedCount);
        GUILayout.EndArea();
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

    // Plain drag with nothing to lay out (no selection, or a click-length drag).
    public static void DrawPlainLine(Vector2 guiStart, Vector2 guiEnd)
    {
        DrawGuiLine(guiStart, guiEnd, Color.white);
    }

    // The order line with a dot at each position a selected troop will take. Orange means the spacing was clamped.
    public static void DrawLayout(Vector2 guiStart, Vector2 guiLineEnd, bool clamped, IReadOnlyList<Vector2> guiDots)
    {
        Color lineColor = clamped ? ClampedLineColor : LineColor;
        DrawGuiLine(guiStart, guiLineEnd, lineColor);

        Color previous = GUI.color;
        GUI.color = lineColor;
        Texture2D dot = DotTexture();
        foreach (Vector2 gui in guiDots)
            GUI.DrawTexture(new Rect(gui.x - DotSize * 0.5f, gui.y - DotSize * 0.5f, DotSize, DotSize), dot);
        GUI.color = previous;
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

    static void DrawGuiLine(Vector2 guiStart, Vector2 guiEnd, Color color)
    {
        Vector2 delta = guiEnd - guiStart;
        if (delta.sqrMagnitude < 1f) return;

        Matrix4x4 previousMatrix = GUI.matrix;
        Color previousColor = GUI.color;
        GUI.color = color;
        GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, guiStart);
        GUI.DrawTexture(new Rect(guiStart.x, guiStart.y - 1f, delta.magnitude, 2f), Texture2D.whiteTexture);
        GUI.matrix = previousMatrix;
        GUI.color = previousColor;
    }
}
