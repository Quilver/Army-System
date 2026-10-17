using System.Collections.Generic;
using UnityEngine;

// The two endpoint links that join a troop's battle line to allied troops. End 0 is the start, end 1 the end.
public sealed class TroopLineLinks
{
    readonly TroopEndpointLink[] _links = new TroopEndpointLink[2];

    public IReadOnlyList<TroopEndpointLink> All => _links;
    public bool HasFreeEnd => _links[0] == null || _links[1] == null;

    public TroopEndpointLink Get(int end) => _links[Mathf.Clamp(end, 0, 1)];

    public void Set(int end, Troop other, int otherEnd)
    {
        _links[Mathf.Clamp(end, 0, 1)] = new TroopEndpointLink { Other = other, OtherEnd = Mathf.Clamp(otherEnd, 0, 1) };
    }

    public void Clear(int end) => _links[Mathf.Clamp(end, 0, 1)] = null;

    public void SetTension(int end, Vector2 tension)
    {
        var link = Get(end);
        if (link != null) link.Tension = tension;
    }
}