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

    public static Vector2 ToGui(Vector2 screenPosition) => new(screenPosition.x, Screen.height - screenPosition.y);

    // World position in GUI space (origin top-left).
    public static Vector2 WorldToGui(Camera camera, Vector2 worldPosition)
    {
        Vector3 screen = camera.WorldToScreenPoint(worldPosition);
        return new Vector2(screen.x, Screen.height - screen.y);
    }

    public static bool OverHud(Vector2 screenPosition)
    {
        float guiY = Screen.height - screenPosition.y;
        return screenPosition.x <= 500f
            && (guiY <= 140f || guiY >= Screen.height - 86f);
    }
}
