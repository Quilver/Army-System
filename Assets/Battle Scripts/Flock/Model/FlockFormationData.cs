using System;
using UnityEngine;

[Serializable]
public sealed class FlockFormationData
{
    public Vector2 Start;
    public Vector2 End;
    public Vector2 Facing;
    public bool IsThreatFacing;
    public bool StartLinked;
    public bool EndLinked;

    public Vector2 Tangent
    {
        get
        {
            Vector2 tangent = End - Start;
            return tangent.sqrMagnitude > 0.0001f ? tangent.normalized : Vector2.up;
        }
    }
}

[Serializable]
public sealed class FlockEndpointLink
{
    public Flock Other;
    [Range(0, 1)] public int OtherEnd;
    public Vector2 Tension;
}
