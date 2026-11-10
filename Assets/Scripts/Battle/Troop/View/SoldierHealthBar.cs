using System.Collections.Generic;
using UnityEngine;

// A small HP bar just above the soldier under the cursor (spec §4.6), drawn with IMGUI by TroopPlayerOrders in Play mode.
// The whole bar is the soldier's MaxHP. Left to right:
//   green  current HP
//   red    Bruise (heals over time)
//   black  Bleed (permanent for the battle)
// Any team's soldiers can be hovered. Only living soldiers are picked.
public sealed class SoldierHealthBar
{
    const float PickRadiusScale = 1.25f;      // the cursor picks a soldier within this many soldier radii of its centre
    const float MinWidth = 30f;               // pixels; the bar is never narrower than this when zoomed out
    const float WidthPerDiameter = 1.6f;      // otherwise the bar is this many soldier diameters wide on screen
    const float ReferenceHeight = 1080f;
    const float BarHeight = 7f;               // pixels at a 1080 px screen
    const float HeadGap = 3f;                 // pixels between the top of the soldier and the bar
    const float Border = 1f;

    static readonly Color HealthColor = new(0.2f, 0.85f, 0.25f);
    static readonly Color BruiseColor = new(0.9f, 0.15f, 0.15f);
    static readonly Color BleedColor = Color.black;
    static readonly Color BorderColor = new(1f, 1f, 1f, 0.85f);

    Troop _troop;
    SoldierAgentState _soldier;

    // mouseWorld is null while the cursor is over the HUD.
    public void Update(IReadOnlyList<Troop> troops, Vector2? mouseWorld)
    {
        _troop = null;
        _soldier = null;
        if (!mouseWorld.HasValue) return;

        float pick = TroopConstants.SoldierRadius * PickRadiusScale;
        float best = pick * pick;
        foreach (var troop in troops)
        {
            if (troop == null) continue;
            foreach (var soldier in troop.Soldiers)
            {
                if (!soldier.IsAlive) continue;
                float distance = (troop.WorldPosition(soldier) - mouseWorld.Value).sqrMagnitude;
                if (distance >= best) continue;
                best = distance;
                _troop = troop;
                _soldier = soldier;
            }
        }
    }

    public void Draw(Camera camera)
    {
        if (Event.current.type != EventType.Repaint) return;
        if (camera == null || _troop == null || _soldier == null || !_soldier.IsAlive) return;

        Vector2 world = _troop.WorldPosition(_soldier);
        Vector2 centre = TroopScreenSpace.WorldToGui(camera, world);
        Vector2 top = TroopScreenSpace.WorldToGui(camera, world + Vector2.up * TroopConstants.SoldierRadius);
        float radius = Mathf.Abs(centre.y - top.y);
        float scale = Mathf.Max(0.8f, Screen.height / ReferenceHeight);
        float width = Mathf.Max(MinWidth * scale, 2f * radius * WidthPerDiameter);
        float height = BarHeight * scale;
        var outer = new Rect(centre.x - width * 0.5f, centre.y - radius - HeadGap * scale - height, width, height);
        Fill(outer, BorderColor);

        var inner = new Rect(outer.x + Border, outer.y + Border, outer.width - 2f * Border, outer.height - 2f * Border);
        SoldierHealth health = _soldier.Health;
        float max = Mathf.Max(1, health.MaxHp);
        float current = Mathf.Clamp01(health.CurrentHp / max) * inner.width;
        float bruise = Mathf.Clamp01(health.Bruise / max) * inner.width;
        float bleed = inner.width - current - bruise;   // the rest of MaxHP is bleed
        Fill(new Rect(inner.x, inner.y, current, inner.height), HealthColor);
        Fill(new Rect(inner.x + current, inner.y, bruise, inner.height), BruiseColor);
        if (bleed > 0f) Fill(new Rect(inner.x + current + bruise, inner.y, bleed, inner.height), BleedColor);
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
