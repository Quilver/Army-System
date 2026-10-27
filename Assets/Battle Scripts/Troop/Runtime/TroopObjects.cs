using System.Collections.Generic;
using UnityEngine;

// Destroys troop objects. In Play mode this is Object.Destroy (end of frame). Outside Play mode (tests simulate battles
// in Edit mode) the object is destroyed when the simulated frame ends (FlushPending), so a troop is never destroyed in
// the middle of its own update, the same as in Play mode.
public static class TroopObjects
{
    static readonly List<Object> Pending = new();

    public static void Destroy(Object target)
    {
        if (target == null) return;
        if (Application.isPlaying) Object.Destroy(target);
        else if (!Pending.Contains(target)) Pending.Add(target);
    }

    public static void FlushPending()
    {
        foreach (var target in Pending)
            if (target != null) Object.DestroyImmediate(target);
        Pending.Clear();
    }
}
