using System.Collections.Generic;
using UnityEngine;

// Values recorded by the combat systems for the overlay and gizmos. Written only while TroopDebugOverlay is enabled, and
// read only by TroopDebugOverlay. Systems never read it back, so gameplay does not depend on it.
public static class TroopDebugSnapshot
{
    public const float ClashMarkerLifetime = 1f;

    public struct BarrierRecord
    {
        public Troop First;
        public Troop Second;
        public Vector2 Axis;        // from First toward Second
        public float Penetration;   // how far the footprints overlap the barrier gap, in world units
        public float FirstShare;
        public float SecondShare;
        public int Frame;
    }

    public struct ClashRecord
    {
        public Vector2 Position;
        public float Time;
    }

    public static bool Enabled;

    static readonly List<BarrierRecord> _barriers = new();
    static readonly List<ClashRecord> _clashes = new();

    public static IReadOnlyList<BarrierRecord> Barriers => _barriers;
    public static IReadOnlyList<ClashRecord> Clashes => _clashes;

    public static void RecordBarrier(Troop first, Troop second, Vector2 axis, float penetration, float firstShare, float secondShare)
    {
        if (!Enabled) return;
        int frame = Time.frameCount;
        for (int i = _barriers.Count - 1; i >= 0; i--)
            if (_barriers[i].Frame < frame - 1) _barriers.RemoveAt(i);
        _barriers.Add(new BarrierRecord
        {
            First = first,
            Second = second,
            Axis = axis,
            Penetration = penetration,
            FirstShare = firstShare,
            SecondShare = secondShare,
            Frame = frame,
        });
    }

    public static void RecordClash(Vector2 position)
    {
        if (!Enabled) return;
        float now = Time.time;
        for (int i = _clashes.Count - 1; i >= 0; i--)
            if (now - _clashes[i].Time > ClashMarkerLifetime) _clashes.RemoveAt(i);
        _clashes.Add(new ClashRecord { Position = position, Time = now });
    }

    public static void Clear()
    {
        _barriers.Clear();
        _clashes.Clear();
    }
}
