using System;
using UnityEngine;

[Serializable]
public sealed class TroopFormationData
{
    public Vector2 Start;
    public Vector2 End;
    public Vector2 Facing;
    public bool IsThreatFacing;
    public bool StartLinked;
    public bool EndLinked;

    // SU-161: Start to End is the front rank. The ranks stand behind it, along -Front.
    // Unit normal of the line on the side of Facing (Facing itself unless a chain joint folded the line).
    public Vector2 Front => TroopFootprint.RectFacing(Start, End, Facing);

    public float Length => Vector2.Distance(Start, End);

    // From the line (front rank) to the middle of the ranks of a troop with this many living soldiers.
    public Vector2 RanksOffset(int livingSoldiers) => -Front * LineGeometry.FrontRankOffset(Length, livingSoldiers);

    public Vector2 Tangent
    {
        get
        {
            Vector2 tangent = End - Start;
            return tangent.sqrMagnitude > 0.0001f ? tangent.normalized : Vector2.up;
        }
    }
}
