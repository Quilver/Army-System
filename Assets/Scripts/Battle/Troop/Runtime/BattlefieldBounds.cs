using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

// The edge of the battlefield: the bounding rectangle of the baked NavMesh in the active scene (SU-74). Maps without
// boundary walls (Level 2) rely on it to keep troops on the map. Without a NavMesh there is no edge and nothing is
// clamped; the lookup is retried every few seconds in case the NavMesh is added later.
public static class BattlefieldBounds
{
    const float RetrySeconds = 2f;

    static Rect _rect;
    static bool _hasRect;
    static int _sceneHandle = int.MinValue;
    static float _nextAttempt;

    public static bool TryGet(out Rect rect)
    {
        int scene = SceneManager.GetActiveScene().handle;
        if (scene != _sceneHandle)
        {
            _sceneHandle = scene;
            _hasRect = false;
            _nextAttempt = 0f;
        }
        if (!_hasRect && Time.realtimeSinceStartup >= _nextAttempt)
        {
            _nextAttempt = Time.realtimeSinceStartup + RetrySeconds;
            _hasRect = TryMeasureNavMesh(out _rect);
        }
        rect = _rect;
        return _hasRect;
    }

    // Forgets the measured edge (after re-baking the NavMesh at runtime, for example).
    public static void Invalidate()
    {
        _hasRect = false;
        _nextAttempt = 0f;
    }

    // Limits a pivot displacement so the pivot stays at least inset inside the edge. Each axis is limited on its own, so
    // a troop running into the edge slides along it. A pivot already outside may move back in but not further out.
    public static Vector2 ClampStep(Vector2 start, Vector2 delta, float inset)
    {
        if (!TryGet(out Rect rect)) return delta;
        ClampAxis(start.x, ref delta.x, rect.xMin, rect.xMax, inset);
        ClampAxis(start.y, ref delta.y, rect.yMin, rect.yMax, inset);
        return delta;
    }

    static void ClampAxis(float start, ref float delta, float min, float max, float inset)
    {
        inset = Mathf.Clamp(inset, 0f, Mathf.Max(0f, (max - min) * 0.5f));
        min += inset;
        max -= inset;
        float next = start + delta;
        if (delta < 0f && next < min) delta = start > min ? min - start : 0f;
        else if (delta > 0f && next > max) delta = start < max ? max - start : 0f;
    }

    static bool TryMeasureNavMesh(out Rect rect)
    {
        rect = default;
        NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
        Vector3[] vertices = triangulation.vertices;
        if (vertices == null || vertices.Length == 0) return false;
        Vector2 min = vertices[0], max = vertices[0];
        foreach (Vector3 vertex in vertices)
        {
            min = Vector2.Min(min, vertex);
            max = Vector2.Max(max, vertex);
        }
        if (max.x - min.x <= 0.01f || max.y - min.y <= 0.01f) return false;
        rect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        return true;
    }
}
