using System.Collections.Generic;
using UnityEngine;

// Draws projectiles in flight as short pooled sprites (spec §4.7), each with a fading trail in its shooter's army colour
// (SU-142). Renderers are reused and the trails share one mesh, so shots do not allocate.
public sealed class ProjectileView
{
    // 50% larger than the original 0.6 x 0.06 (SU-142).
    const float Length = 0.9f;
    const float Thickness = 0.09f;
    // The trail runs back from the missile's tail; it is shorter while the missile is close to where it was fired.
    const float TrailLength = 1.6f;
    const float TrailHeadAlpha = 0.7f;
    // Width at the tail end of the trail, as a fraction of the missile's thickness.
    const float TrailTailWidth = 0.25f;
    // On the Unit layer, just under the missiles (2).
    const int TrailSortingOrder = 1;
    const string TrailShaderPath = "Shaders/TroopOverlayColor";

    readonly List<SpriteRenderer> _renderers = new();
    readonly List<Vector3> _trailVertices = new();
    readonly List<Color> _trailColors = new();
    readonly List<int> _trailTriangles = new();
    Transform _root;
    Mesh _trailMesh;
    Material _trailMaterial;
    MeshRenderer _trailRenderer;

    public void Draw(IReadOnlyList<TroopProjectiles.Projectile> projectiles, Transform parent)
    {
        if (_root == null)
        {
            _root = new GameObject("Projectiles").transform;
            _root.SetParent(parent, false);
        }
        EnsureTrails();
        while (_renderers.Count < projectiles.Count) _renderers.Add(Create());

        _trailVertices.Clear();
        _trailColors.Clear();
        _trailTriangles.Clear();

        for (int i = 0; i < _renderers.Count; i++)
        {
            SpriteRenderer renderer = _renderers[i];
            bool used = i < projectiles.Count;
            renderer.enabled = used;
            if (!used) continue;
            var projectile = projectiles[i];
            Vector2 direction = projectile.To - projectile.From;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Vector2 position = projectile.Position;
            renderer.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, angle));
            Color color = projectile.Shooter != null ? projectile.Shooter.ArmyColor : Color.white;
            renderer.color = color;
            AddTrail(projectile.From, position, direction, color);
        }

        if (_trailMesh != null)
        {
            _trailMesh.Clear();
            _trailMesh.SetVertices(_trailVertices);
            _trailMesh.SetColors(_trailColors);
            _trailMesh.SetTriangles(_trailTriangles, 0);
            _trailMesh.RecalculateBounds();
            _trailRenderer.enabled = _trailTriangles.Count > 0;
        }
    }

    public void Destroy()
    {
        if (_root != null) TroopObjects.Destroy(_root.gameObject);
        if (_trailMesh != null) TroopObjects.Destroy(_trailMesh);
        if (_trailMaterial != null) TroopObjects.Destroy(_trailMaterial);
        _renderers.Clear();
        _trailMesh = null;
        _trailMaterial = null;
        _trailRenderer = null;
    }

    // A tapered quad from the missile's tail back along its path, opaque-ish at the missile and clear at the far end.
    void AddTrail(Vector2 from, Vector2 position, Vector2 direction, Color color)
    {
        if (_trailRenderer == null || direction.sqrMagnitude < 1e-8f) return;
        Vector2 forward = direction.normalized;
        Vector2 head = position - forward * (Length * 0.5f);
        float travelled = Vector2.Dot(head - from, forward);
        float length = Mathf.Min(TrailLength, travelled);
        if (length <= 0.01f) return;
        Vector2 tail = head - forward * length;
        Vector2 side = new(-forward.y, forward.x);
        float headHalf = Thickness * 0.5f;
        float tailHalf = headHalf * TrailTailWidth;

        int start = _trailVertices.Count;
        _trailVertices.Add(Local(head + side * headHalf));
        _trailVertices.Add(Local(head - side * headHalf));
        _trailVertices.Add(Local(tail + side * tailHalf));
        _trailVertices.Add(Local(tail - side * tailHalf));
        Color headColor = new(color.r, color.g, color.b, TrailHeadAlpha);
        Color tailColor = new(color.r, color.g, color.b, 0f);
        _trailColors.Add(headColor);
        _trailColors.Add(headColor);
        _trailColors.Add(tailColor);
        _trailColors.Add(tailColor);
        _trailTriangles.Add(start);
        _trailTriangles.Add(start + 1);
        _trailTriangles.Add(start + 3);
        _trailTriangles.Add(start);
        _trailTriangles.Add(start + 3);
        _trailTriangles.Add(start + 2);
    }

    Vector3 Local(Vector2 world) => _root.InverseTransformPoint(world);

    void EnsureTrails()
    {
        if (_trailRenderer != null) return;
        Shader shader = Resources.Load<Shader>(TrailShaderPath);
        if (shader == null) return;
        _trailMaterial = new Material(shader) { name = "Projectile Trails", hideFlags = HideFlags.DontSave };
        _trailMesh = new Mesh { name = "Projectile Trails" };
        _trailMesh.MarkDynamic();

        var trails = new GameObject("Projectile Trails");
        trails.transform.SetParent(_root, false);
        trails.AddComponent<MeshFilter>().sharedMesh = _trailMesh;
        _trailRenderer = trails.AddComponent<MeshRenderer>();
        _trailRenderer.sharedMaterial = _trailMaterial;
        _trailRenderer.sortingLayerName = "Unit";
        _trailRenderer.sortingOrder = TrailSortingOrder;
        _trailRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _trailRenderer.receiveShadows = false;
    }

    SpriteRenderer Create()
    {
        var shot = new GameObject("Projectile");
        shot.transform.SetParent(_root, false);
        shot.transform.localScale = new Vector3(Length, Thickness, 1f);
        var renderer = shot.AddComponent<SpriteRenderer>();
        renderer.sprite = SoldierSpriteLibrary.Square;
        renderer.sharedMaterial = SoldierSpriteLibrary.ModelMaterial;
        renderer.sortingLayerName = "Unit";
        renderer.sortingOrder = 2;
        return renderer;
    }
}
