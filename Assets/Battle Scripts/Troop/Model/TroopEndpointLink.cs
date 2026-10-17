using System;
using UnityEngine;

[Serializable]
public sealed class TroopEndpointLink
{
    public Troop Other;
    [Range(0, 1)] public int OtherEnd;
    public Vector2 Tension;
}
