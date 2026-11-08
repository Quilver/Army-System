using System.Collections.Generic;
using UnityEngine;

// Transparent mask of where a ranged troop can shoot, shown while the cursor hovers over the troop (any team).
// It is drawn from each soldier that can shoot, using the same reach test as targeting (RangedRules.CanReach,
// Decisions D-54): a sector around the shooter with its weapon range, opening within RangedFrontArcDeg of the troop's
// facing. The mask is the union of the sectors with an outline, and its fill is a heat map: the more fire (fire rate)
// that can reach a point, the darker and more opaque it is. The shader (Resources/Shaders/TroopRangeMask.shader) does
// the per-pixel work; this class feeds it the shooters each frame and sizes a quad over their reach.
// Troops without a soldier that can shoot show nothing. Owned and updated by TroopPlayerOrders.
// Line of sight (SU-136) is not drawn: a direct-fire weapon's sector still covers ground behind troops and sight-blocking
// terrain, which it cannot shoot at.
public sealed class TroopRangeMask
{
    // Must match MAX_SHOOTERS in the shader. Extra shooters are left out of the mask.
    public const int MaxShooters = 128;
    const string SortingLayer = "Terrain";
    // Above the troop radius (10) and pivot (11) sprites, below the soldiers (sorting layer "Unit").
    const int SortingOrder = 12;

    static readonly int ShootersId = Shader.PropertyToID("_Shooters");
    static readonly int ShooterCountId = Shader.PropertyToID("_ShooterCount");
    static readonly int TotalWeightId = Shader.PropertyToID("_TotalWeight");
    static readonly int FacingId = Shader.PropertyToID("_Facing");
    static readonly int FillColorId = Shader.PropertyToID("_FillColor");
    static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
    static readonly int OutlinePixelsId = Shader.PropertyToID("_OutlinePixels");
    static readonly int MinFillAlphaId = Shader.PropertyToID("_MinFillAlpha");
    static readonly int MaxFillAlphaId = Shader.PropertyToID("_MaxFillAlpha");
    static readonly int HeatDarkenId = Shader.PropertyToID("_HeatDarken");

    readonly List<Vector4> _reach = new();
    readonly Vector4[] _shaderShooters = new Vector4[MaxShooters];
    readonly Vector3[] _corners = new Vector3[4];
    GameObject _root;
    Mesh _mesh;
    Material _material;

    public struct Style
    {
        public Color Fill;            // fill colour where little fire reaches
        public float MinFillAlpha;    // fill opacity where one shooter's fire reaches (out of the whole troop's)
        public float MaxFillAlpha;    // fill opacity where all of the troop's fire reaches
        public float HeatDarken;      // 0-1: how much darker the fill gets where all fire reaches
        public Color Outline;
        public float OutlinePixels;
    }

    // Shows the mask for the troop, or hides it when the troop is null, gone, or has no soldier that can shoot.
    public void Update(Troop troop, Style style)
    {
        if (troop == null || troop.RangedFire == null)
        {
            SetVisible(false);
            return;
        }
        troop.RangedFire.CollectReach(_reach);
        if (_reach.Count == 0)
        {
            SetVisible(false);
            return;
        }

        EnsureObjects();
        int count = Mathf.Min(_reach.Count, MaxShooters);
        float totalWeight = 0f;
        Vector2 min = new(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new(float.NegativeInfinity, float.NegativeInfinity);
        for (int i = 0; i < count; i++)
        {
            Vector4 shooter = _reach[i];
            _shaderShooters[i] = shooter;
            totalWeight += shooter.w;
            min = Vector2.Min(min, new Vector2(shooter.x - shooter.z, shooter.y - shooter.z));
            max = Vector2.Max(max, new Vector2(shooter.x + shooter.z, shooter.y + shooter.z));
        }
        for (int i = count; i < MaxShooters; i++) _shaderShooters[i] = Vector4.zero;

        // A small margin so the anti-aliased edge is not clipped by the quad.
        Vector2 margin = (max - min) * 0.02f + Vector2.one * 0.5f;
        min -= margin;
        max += margin;
        _corners[0] = new Vector3(min.x, min.y, 0f);
        _corners[1] = new Vector3(min.x, max.y, 0f);
        _corners[2] = new Vector3(max.x, max.y, 0f);
        _corners[3] = new Vector3(max.x, min.y, 0f);
        _mesh.vertices = _corners;
        _mesh.RecalculateBounds();

        float halfArc = Mathf.Clamp(TroopTuning.Active.RangedFrontArcDeg, 0f, 180f) * Mathf.Deg2Rad;
        Vector2 facing = troop.Facing;
        _material.SetVectorArray(ShootersId, _shaderShooters);
        _material.SetFloat(ShooterCountId, count);
        _material.SetFloat(TotalWeightId, totalWeight);
        _material.SetVector(FacingId, new Vector4(facing.x, facing.y, Mathf.Sin(halfArc), Mathf.Cos(halfArc)));
        _material.SetColor(FillColorId, style.Fill);
        _material.SetColor(OutlineColorId, style.Outline);
        _material.SetFloat(OutlinePixelsId, style.OutlinePixels);
        _material.SetFloat(MinFillAlphaId, style.MinFillAlpha);
        _material.SetFloat(MaxFillAlphaId, style.MaxFillAlpha);
        _material.SetFloat(HeatDarkenId, style.HeatDarken);
        SetVisible(true);
    }

    public void Hide() => SetVisible(false);

    public void Destroy()
    {
        DestroyObject(_mesh);
        DestroyObject(_material);
        DestroyObject(_root);
        _mesh = null;
        _material = null;
        _root = null;
    }

    static void DestroyObject(Object target)
    {
        if (target == null) return;
        if (Application.isPlaying) Object.Destroy(target);
        else Object.DestroyImmediate(target);
    }

    void SetVisible(bool visible)
    {
        if (_root != null && _root.activeSelf != visible) _root.SetActive(visible);
    }

    void EnsureObjects()
    {
        if (_root != null) return;
        Shader shader = Resources.Load<Shader>("Shaders/TroopRangeMask");
        _material = new Material(shader) { name = "Troop Range Mask", hideFlags = HideFlags.DontSave };

        _root = new GameObject("Troop Range Mask");
        _mesh = new Mesh { name = "Troop Range Mask" };
        _mesh.MarkDynamic();
        _mesh.vertices = _corners;
        _mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        _root.AddComponent<MeshFilter>().sharedMesh = _mesh;
        var renderer = _root.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = _material;
        renderer.sortingLayerName = SortingLayer;
        renderer.sortingOrder = SortingOrder;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }
}
