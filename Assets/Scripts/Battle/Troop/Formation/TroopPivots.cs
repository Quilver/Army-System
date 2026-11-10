using UnityEngine;

public static class TroopPivots
{
    public static float Distance(Troop a, Troop b) => Vector2.Distance(a.transform.position, b.transform.position);
}
