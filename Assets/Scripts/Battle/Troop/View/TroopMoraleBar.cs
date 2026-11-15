using System.Collections.Generic;
using UnityEngine;

// A floating Effective Morale bar over every troop on the field (spec §5, §6.4), drawn with IMGUI by TroopPlayerOrders.
// The bar's length is the troop's Effective Morale: full length at its initial Effective Morale, shrinking as soldiers
// die and as retreats and surrounded breaks lower it. Inside, Shock fills from the left in red on the same scale, so
// the troop breaks when the red reaches the right end (Shock > Effective Morale, spec §5.3). The border is the army
// colour. Routed troops, troops that have left the field and troops with no living soldiers show no bar.
public sealed class TroopMoraleBar
{
    const float ReferenceHeight = 1080f;
    const float FullWidth = 64f;              // pixels at a 1080 px screen, at the troop's initial Effective Morale
    const float BarHeight = 6f;               // pixels at a 1080 px screen
    const float Gap = 6f;                     // pixels between the top of the troop's outline and the bar
    const float Border = 1f;
    const float MinInnerWidth = 2f;           // a troop with almost no morale left still shows a sliver

    static readonly Color MoraleColor = new(0.22f, 0.48f, 1f, 0.9f);     // the morale left before a break
    static readonly Color ShockColor = new(0.9f, 0.18f, 0.18f, 1f);

    readonly List<Vector2> _polygon = new();

    // The bar's inner width for this Effective Morale (full width at the initial value, never wider), and how much of
    // it Shock fills from the left (all of it once Shock reaches Effective Morale).
    public static void Layout(float fullWidth, float effectiveMorale, float initialEffectiveMorale, float shock,
        out float barWidth, out float shockWidth)
    {
        float initial = Mathf.Max(1f, initialEffectiveMorale);
        float morale = Mathf.Max(0f, effectiveMorale);
        barWidth = fullWidth * Mathf.Clamp01(morale / initial);
        shockWidth = morale > 0f ? barWidth * Mathf.Clamp01(Mathf.Max(0f, shock) / morale) : barWidth;
    }

    public static bool Shows(Troop troop) =>
        troop != null && troop.isActiveAndEnabled && !troop.IsRouted && !troop.HasLeftField && troop.LivingSoldierCount > 0;

    public void Draw(Camera camera, IReadOnlyList<Troop> troops)
    {
        if (Event.current.type != EventType.Repaint) return;
        if (camera == null || troops == null) return;
        float scale = Mathf.Max(0.8f, Screen.height / ReferenceHeight);
        foreach (var troop in troops)
            if (Shows(troop)) DrawBar(camera, troop, scale);
    }

    void DrawBar(Camera camera, Troop troop, float scale)
    {
        TroopFootprint outline = troop.Border;
        TroopOutlinePolygon.Build(outline, _polygon);
        if (_polygon.Count == 0) return;
        float topY = float.NegativeInfinity;
        foreach (var point in _polygon) topY = Mathf.Max(topY, point.y);

        Vector2 anchor = TroopScreenSpace.WorldToGui(camera, new Vector2(outline.Center.x, topY));
        Layout(FullWidth * scale, troop.EffectiveMorale, troop.InitialEffectiveMorale, troop.Shock,
            out float barWidth, out float shockWidth);
        barWidth = Mathf.Max(MinInnerWidth, barWidth);
        shockWidth = Mathf.Min(shockWidth, barWidth);

        float height = BarHeight * scale;
        float outerWidth = barWidth + 2f * Border;
        var outer = new Rect(anchor.x - outerWidth * 0.5f, anchor.y - Gap * scale - height, outerWidth, height);
        if (outer.yMax < 0f || outer.y > Screen.height || outer.xMax < 0f || outer.x > Screen.width) return;

        Color army = troop.ArmyColor;
        Fill(outer, new Color(army.r, army.g, army.b, 1f));
        var inner = new Rect(outer.x + Border, outer.y + Border, barWidth, outer.height - 2f * Border);
        Fill(new Rect(inner.x + shockWidth, inner.y, inner.width - shockWidth, inner.height), MoraleColor);
        Fill(new Rect(inner.x, inner.y, shockWidth, inner.height), ShockColor);
    }

    static void Fill(Rect rect, Color color)
    {
        if (rect.width <= 0f || rect.height <= 0f) return;
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }
}
