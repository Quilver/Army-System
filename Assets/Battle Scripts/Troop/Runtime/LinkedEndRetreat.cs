using UnityEngine;

// Soldiers close to a linked line end stay near the joint while the rest of the line folds back (accordion retreat),
// so the link to the neighbouring troop is maintained.
public static class LinkedEndRetreat
{
    const float LinkedEndPinZone = 0.3f;
    const float LinkedEndRetreatReduction = 0.85f;

    // Fraction (0-1] of the retreat distance this soldier should travel.
    public static float ScaleFor(Troop troop, SoldierAgentState soldier)
    {
        bool startLinked = troop.GetLineLink(0) != null;
        bool endLinked = troop.GetLineLink(1) != null;
        TroopFormationData formation = troop.Formation;
        if (formation == null || (!startLinked && !endLinked)) return 1f;

        Vector2 line = formation.End - formation.Start;
        float lengthSquared = line.sqrMagnitude;
        if (lengthSquared < 0.0001f) return 1f;

        float t = Mathf.Clamp01(Vector2.Dot(troop.WorldPosition(soldier) - formation.Start, line) / lengthSquared);
        float pin = 0f;
        if (startLinked) pin = Mathf.Max(pin, 1f - Mathf.Clamp01(t / LinkedEndPinZone));
        if (endLinked) pin = Mathf.Max(pin, 1f - Mathf.Clamp01((1f - t) / LinkedEndPinZone));
        pin = pin * pin * (3f - 2f * pin);
        return 1f - LinkedEndRetreatReduction * pin;
    }
}