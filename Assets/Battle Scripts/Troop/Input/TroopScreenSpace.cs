using UnityEngine;

// Conversions between screen space (origin bottom-left), GUI space (origin top-left) and world space,
// plus the HUD hit test used to keep clicks on the on-screen help from issuing orders.
public static class TroopScreenSpace
{
    public static Vector2 ScreenToWorld(Camera camera, Vector2 screenPosition)
    {
        float cameraDepth = -camera.transform.position.z;
        Vector3 world = camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, cameraDepth));
        return world;
    }

    // World units covered by a number of screen pixels at a screen position (so pixel thresholds do not depend on zoom).
    public static float PixelsToWorld(Camera camera, Vector2 screenPosition, float pixels)
    {
        Vector2 a = ScreenToWorld(camera, screenPosition);
        Vector2 b = ScreenToWorld(camera, screenPosition + new Vector2(pixels, 0f));
        return Vector2.Distance(a, b);
    }

    public static Vector2 ToGui(Vector2 screenPosition) => new(screenPosition.x, Screen.height - screenPosition.y);

    // World position in GUI space (origin top-left).
    public static Vector2 WorldToGui(Camera camera, Vector2 worldPosition)
    {
        Vector3 screen = camera.WorldToScreenPoint(worldPosition);
        return new Vector2(screen.x, Screen.height - screen.y);
    }

    // The help panel (top centre) and the objectives text (top left). The unit card is tested by TroopSelectionCard.
    public static bool OverHud(Vector2 screenPosition)
    {
        Vector2 gui = ToGui(screenPosition);
        if (TroopOrderOverlay.HelpRect.Contains(gui)) return true;
        return gui.x <= 500f && gui.y <= 140f;
    }
}
