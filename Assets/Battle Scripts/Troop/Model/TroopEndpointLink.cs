using System;
using UnityEngine;

// One end of a chain link (spec §3.4): the neighbour troop and which of its line points this line point is joined to.
//   Tension  gap between the two troops' natural line points (this side's view), used by AlliedLinkTension.
//   Joint    the shared joint J solved this frame.
//   Pending  a link created by a multi-troop line order (spec §6.2, Q-19). It reserves both line points but has no
//            geometry until both troops have reached their places, so marching troops are not dragged into line.
[Serializable]
public sealed class TroopEndpointLink
{
    public Troop Other;
    [Range(0, 1)] public int OtherEnd;
    public Vector2 Tension;
    public Vector2 Joint;
    public bool Pending;
}
