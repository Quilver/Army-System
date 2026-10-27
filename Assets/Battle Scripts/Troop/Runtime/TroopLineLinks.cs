using System.Collections.Generic;
using UnityEngine;

// The two line points that can join a troop's battle line to allied troops (spec §3.4). End 0 is the right wing, end 1
// the left wing (see TroopConstants). Also holds the relaxation of a stretched line point after its link breaks
// (spec §3.4.1 endpoint snap-back damping).
public sealed class TroopLineLinks
{
    readonly TroopEndpointLink[] _links = new TroopEndpointLink[TroopConstants.MaxChainLinks];
    readonly Vector2[] _relaxOffsets = new Vector2[TroopConstants.MaxChainLinks];

    public IReadOnlyList<TroopEndpointLink> All => _links;
    public bool HasFreeEnd => _links[0] == null || _links[1] == null;
    public bool HasAnyLink => _links[0] != null || _links[1] != null;
    public bool HasActiveLink => IsActive(0) || IsActive(1);

    public TroopEndpointLink Get(int end) => _links[Mathf.Clamp(end, 0, 1)];

    public bool IsActive(int end)
    {
        TroopEndpointLink link = Get(end);
        return link != null && link.Other != null && !link.Pending;
    }

    public void Set(int end, Troop other, int otherEnd, bool pending)
    {
        int index = Mathf.Clamp(end, 0, 1);
        _links[index] = new TroopEndpointLink { Other = other, OtherEnd = Mathf.Clamp(otherEnd, 0, 1), Pending = pending };
        _relaxOffsets[index] = Vector2.zero;
    }

    public void Clear(int end) => _links[Mathf.Clamp(end, 0, 1)] = null;

    public void SetTension(int end, Vector2 tension, Vector2 joint)
    {
        var link = Get(end);
        if (link == null) return;
        link.Tension = tension;
        link.Joint = joint;
    }

    // Offset of a free line point from its natural position, shrinking toward zero after the link broke.
    public Vector2 RelaxOffset(int end) => _relaxOffsets[Mathf.Clamp(end, 0, 1)];

    public void StartRelax(int end, Vector2 offset) => _relaxOffsets[Mathf.Clamp(end, 0, 1)] = offset;

    public void TickRelax(float deltaTime, float speed)
    {
        for (int i = 0; i < _relaxOffsets.Length; i++)
        {
            Vector2 offset = _relaxOffsets[i];
            float length = offset.magnitude;
            if (length <= 0f) continue;
            float next = Mathf.Max(0f, length - speed * deltaTime);
            _relaxOffsets[i] = next <= 1e-5f ? Vector2.zero : offset * (next / length);
        }
    }

    public void ClearRelax()
    {
        for (int i = 0; i < _relaxOffsets.Length; i++) _relaxOffsets[i] = Vector2.zero;
    }
}
