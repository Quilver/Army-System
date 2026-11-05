using UnityEngine;

namespace LeveragedWars.Battle.AI
{
    /// <summary>
    /// Ground-plane helpers. The battle is 2D: the ground is the XY plane and Z (draw depth) is ignored when
    /// comparing positions. A troop's or marker's facing is its transform's right (red arrow), as in the troop system.
    /// Distances are in world units (troop radius 3-8, bow range 10-20).
    /// </summary>
    public static class AIGeometry
    {
        public static Vector3 Flat(Vector3 v) => new Vector3(v.x, v.y, 0f);

        public static float FlatDistance(Vector3 a, Vector3 b) => Flat(a - b).magnitude;

        /// <summary>Flat unit direction from one point to another, or the fallback if they coincide.</summary>
        public static Vector3 Direction(Vector3 from, Vector3 to, Vector3 fallback)
        {
            Vector3 d = Flat(to - from);
            if (d.sqrMagnitude > 0.0001f) return d.normalized;
            Vector3 f = Flat(fallback);
            return f.sqrMagnitude > 0.0001f ? f.normalized : Vector3.right;
        }

        public static Vector3 FlatForward(Vector3 forward) => Direction(Vector3.zero, forward, Vector3.right);

        /// <summary>The facing of a scene marker: its red (right) arrow.</summary>
        public static Vector3 MarkerFacing(Transform marker) => FlatForward(marker.right);

        /// <summary>The direction to the right of a flat facing (a quarter turn clockwise).</summary>
        public static Vector3 Right(Vector3 facing) => new Vector3(facing.y, -facing.x, 0f);
    }

    public static class AIGizmos
    {
        public static void Circle(Vector3 centre, float radius, int segments = 32)
        {
            if (radius <= 0f) return;
            Vector3 previous = centre + new Vector3(radius, 0f, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                Vector3 next = centre + new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f);
                Gizmos.DrawLine(previous, next);
                previous = next;
            }
        }
    }
}
