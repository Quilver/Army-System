using System.Collections.Generic;
using UnityEngine;

// Whether engaged troops show the engagement heat map (TroopEngagementView). F5 toggles it (TroopPlayerOrders).
public enum EngagementViewMode
{
    HeatMap,   // a red band across the gap between engaged lines
    Off
}

// For every pair of engaged troops, a red heat map across the gap between their lines (EngagementHeatStrip): a smooth
// gradient, most intense in the middle of the line gap and fading to nothing at each troop's line and toward the ends
// of the engagement. The number of soldiers fighting does not change it. Built each frame as one vertex-coloured
// mesh (Resources/Shaders/TroopOverlayColor.shader) on the Terrain sorting layer, above the range mask and below the
// soldiers. Owned and updated by TroopPlayerOrders.
public sealed class TroopEngagementView
{
    const string SortingLayer = "Terrain";
    // Above the troop radius (10), pivot (11) and range mask (12); below the soldiers (sorting layer "Unit").
    const int SortingOrder = 13;
    // Columns of the heat mesh along the front (world units). Fine enough that the end fade reads as a smooth gradient.
    const float HeatColumnSpacing = 0.25f;

    public struct Style
    {
        public Color HeatLight;       // colour where the heat is low
        public Color HeatDark;        // colour where the heat is full
        public float MaxHeatAlpha;    // opacity where the heat is full; it falls to 0 with the heat
        public float EndFade;         // world units over which the heat fades at each end of the engagement
        public float ReachSlack;      // flank fallback: engaged-soldier reach beyond EngagementGap + 2 radii (EngagementFrontage)
    }

    readonly List<(Troop first, Troop second)> _pairs = new();
    readonly List<EngagementFrontage.Link> _links = new();
    readonly List<EngagementHeatStrip.Column> _columns = new();
    readonly List<Vector3> _vertices = new();
    readonly List<Color> _colors = new();
    readonly List<int> _triangles = new();
    GameObject _root;
    Mesh _mesh;
    Material _material;

    // Shows every engaged pair among the troops, or nothing when the mode is Off.
    public void Update(IReadOnlyList<Troop> troops, EngagementViewMode mode, Style style)
    {
        if (mode == EngagementViewMode.Off || troops == null)
        {
            SetVisible(false);
            return;
        }

        _vertices.Clear();
        _colors.Clear();
        _triangles.Clear();
        float reach = EngagementFrontage.Reach(TroopTuning.Active.EngagementGap, style.ReachSlack);
        var settings = new EngagementHeatStrip.Settings
        {
            CellWidth = HeatColumnSpacing,
            EndFade = style.EndFade
        };
        EngagementFrontage.CollectEngagedPairs(troops, _pairs);
        foreach (var (first, second) in _pairs)
        {
            _links.Clear();
            EngagementFrontage.Measure(first, second, reach, _links, out _, out _);
            TroopFootprint a = first.Footprint;
            TroopFootprint b = second.Footprint;
            if (!EngagementHeatStrip.Build(a.LinePoint(TroopConstants.RightEnd), a.LinePoint(TroopConstants.LeftEnd),
                    b.LinePoint(TroopConstants.RightEnd), b.LinePoint(TroopConstants.LeftEnd), _links, settings, _columns,
                    out Vector2 origin, out Vector2 along, out Vector2 across))
                continue;
            AddBand(origin, along, across, style);
        }

        if (_vertices.Count == 0)
        {
            SetVisible(false);
            return;
        }
        EnsureObjects();
        _mesh.Clear();
        _mesh.SetVertices(_vertices);
        _mesh.SetColors(_colors);
        _mesh.SetTriangles(_triangles, 0);
        _mesh.RecalculateBounds();
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

    // Read by tests: what the last Update built.
    internal int VertexCount => _vertices.Count;
    internal bool IsVisible => _root != null && _root.activeSelf;

    // A grid over the band: one column of Rows + 1 vertices from the first troop's line to the second's at each column
    // of the strip. Colours are per vertex, so the heat blends smoothly between them.
    void AddBand(Vector2 origin, Vector2 along, Vector2 across, Style style)
    {
        const int rows = EngagementHeatStrip.Rows;
        int first = _vertices.Count;
        foreach (var column in _columns)
        {
            for (int r = 0; r <= rows; r++)
            {
                float v = r / (float)rows;
                float side = Mathf.Lerp(column.SideA, column.SideB, v);
                _vertices.Add(origin + along * column.Along + across * side);
                _colors.Add(HeatColour(EngagementHeatStrip.Intensity(column, v), style));
            }
        }
        for (int c = 0; c + 1 < _columns.Count; c++)
        {
            int left = first + c * (rows + 1);
            int right = left + rows + 1;
            for (int r = 0; r < rows; r++)
                AddQuad(left + r, left + r + 1, right + r + 1, right + r);
        }
    }

    internal static Color HeatColour(float intensity, Style style)
    {
        intensity = Mathf.Clamp01(intensity);
        Color colour = Color.Lerp(style.HeatLight, style.HeatDark, intensity);
        colour.a = style.MaxHeatAlpha * intensity;
        return colour;
    }

    void AddQuad(int a, int b, int c, int d)
    {
        _triangles.Add(a);
        _triangles.Add(b);
        _triangles.Add(c);
        _triangles.Add(a);
        _triangles.Add(c);
        _triangles.Add(d);
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
        Shader shader = Resources.Load<Shader>("Shaders/TroopOverlayColor");
        _material = new Material(shader) { name = "Troop Engagement View", hideFlags = HideFlags.DontSave };

        _root = new GameObject("Troop Engagement View");
        _mesh = new Mesh { name = "Troop Engagement View", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        _mesh.MarkDynamic();
        _root.AddComponent<MeshFilter>().sharedMesh = _mesh;
        var renderer = _root.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = _material;
        renderer.sortingLayerName = SortingLayer;
        renderer.sortingOrder = SortingOrder;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }
}
