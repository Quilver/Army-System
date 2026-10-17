using UnityEngine;

// Working battle-line segment for one troop, rebuilt every frame while allied lines are connected.
// End 0 is Start and end 1 is End.
public sealed class LineSegment
{
    public Vector2 Start;
    public Vector2 End;
    public Vector2 Facing;
    public bool IsThreatFacing;

    public Vector2 Endpoint(int end) => end == 0 ? Start : End;
}
