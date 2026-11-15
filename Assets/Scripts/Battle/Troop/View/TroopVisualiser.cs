using System.Collections.Generic;
using UnityEngine;

// Shows a troop's border (Troop.Border, see TroopBorder, SU-162) and its pivot: a fill in the army colour, with no
// stroke along its edge (SU-143). The fill is always shown (SU-170): enemy troops more strongly than the player's
// (Formation Tuning: Friendly / Enemy Fill Alpha), stronger again while hovered or selected, and soft at the edge: full
// opacity inside, fading to nothing over Fill Edge Fade at the border. The border is the same shape the player can click
// inside to select the troop (TroopSelection).
public sealed class TroopVisualiser : MonoBehaviour
{
    const float PivotSize = 1f;
    // The soft edge is never more than this share of the border's smaller half-size, so a small troop keeps a solid middle.
    const float MaxEdgeFadeShare = 0.6f;
    const int CornerSegments = TroopOutlinePolygon.DefaultCornerSegments;

    // The player's team (set by TroopPlayerOrders): its troops get the friendly fill, every other team the enemy fill.
    public static int PlayerTeamId { get; set; }
    // Below the range mask (12) and engagement view (13); above the terrain.
    const int OutlineSortingOrder = 10;
    const string SortingLayer = "Terrain";
    const string ShaderPath = "Shaders/TroopOverlayColor";

    public Sprite sprite;

    Troop _troop;
    bool _selected;
    bool _hovered;
    // The old radius disc. Kept so existing prefabs still bind; hidden unless _showRadius is set.
    [SerializeField]
    SpriteRenderer _radiusRenderer;
    [SerializeField]
    SpriteRenderer _pivotRenderer;
    [SerializeField]
    bool _showRadius;

    readonly List<Vector2> _outer = new();
    readonly List<Vector2> _inner = new();
    readonly List<Vector3> _vertices = new();
    readonly List<Color> _colors = new();
    readonly List<int> _triangles = new();
    Mesh _mesh;
    Material _material;
    MeshRenderer _outlineRenderer;

    public void Initialize(Troop troop)
    {
        _troop = troop;
        _selected = troop.IsSelected;
        _hovered = troop.IsHovered;
        if (_radiusRenderer == null && _showRadius)
            _radiusRenderer = CreateRenderer("Troop Radius", SoldierSpriteLibrary.Circle, 10);
        if (_pivotRenderer == null)
            _pivotRenderer = CreateRenderer("Troop Pivot", sprite != null ? sprite : SoldierSpriteLibrary.Circle, 11);
        EnsureOutline();
        UpdateVisuals();
    }

    public void SetSelected(bool selected)
    {
        _selected = selected;
        UpdateVisuals();
    }

    public void SetHovered(bool hovered)
    {
        _hovered = hovered;
        UpdateVisuals();
    }

    void LateUpdate()
    {
        UpdateVisuals();
    }

    void OnDestroy()
    {
        DestroyObject(_mesh);
        DestroyObject(_material);
    }

    SpriteRenderer CreateRenderer(string objectName, Sprite rendererSprite, int sortingOrder)
    {
        var visual = new GameObject(objectName);
        visual.transform.SetParent(transform, false);
        var renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = rendererSprite;
        renderer.sharedMaterial = SoldierSpriteLibrary.ModelMaterial;
        renderer.sortingLayerName = SortingLayer;
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    void EnsureOutline()
    {
        if (_outlineRenderer != null) return;
        Shader shader = Resources.Load<Shader>(ShaderPath);
        if (shader == null) return;
        _material = new Material(shader) { name = "Troop Outline", hideFlags = HideFlags.DontSave };
        _mesh = new Mesh { name = "Troop Outline" };
        _mesh.MarkDynamic();

        var outline = new GameObject("Troop Outline");
        outline.transform.SetParent(transform, false);
        outline.AddComponent<MeshFilter>().sharedMesh = _mesh;
        _outlineRenderer = outline.AddComponent<MeshRenderer>();
        _outlineRenderer.sharedMaterial = _material;
        _outlineRenderer.sortingLayerName = SortingLayer;
        _outlineRenderer.sortingOrder = OutlineSortingOrder;
        _outlineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _outlineRenderer.receiveShadows = false;
    }

    void UpdateVisuals()
    {
        if (_troop == null || _pivotRenderer == null) return;

        Color armyColor = _troop.ArmyColor;
        float fillAlpha = FillAlpha(TroopTuning.Active, _troop.TeamId == PlayerTeamId, _hovered, _selected);

        if (_radiusRenderer != null)
        {
            _radiusRenderer.enabled = _showRadius;
            if (_showRadius)
            {
                _radiusRenderer.color = new Color(armyColor.r, armyColor.g, armyColor.b, fillAlpha);
                _radiusRenderer.transform.localPosition = Vector3.zero;
                _radiusRenderer.transform.localScale = Vector3.one * (_troop.Radius * 2f);
            }
        }

        UpdateOutline(armyColor, fillAlpha);

        _pivotRenderer.color = _selected ? Color.Lerp(armyColor, Color.white, 0.55f) : armyColor;
        _pivotRenderer.transform.localPosition = Vector3.zero;
        _pivotRenderer.transform.localScale = Vector3.one * PivotSize;
    }

    // The border's fill opacity (SU-170).
    public static float FillAlpha(TroopTuning tuning, bool friendly, bool hovered, bool selected)
    {
        float alpha = friendly ? tuning.FriendlyFillAlpha : tuning.EnemyFillAlpha;
        if (selected) alpha += tuning.SelectedFillAlphaBonus;
        else if (hovered) alpha += tuning.HoverFillAlphaBonus;
        return Mathf.Clamp01(alpha);
    }

    void UpdateOutline(Color armyColor, float fillAlpha)
    {
        if (_outlineRenderer == null) return;
        bool visible = fillAlpha > 0f && !_troop.HasLeftField;
        if (_outlineRenderer.enabled != visible) _outlineRenderer.enabled = visible;
        if (!visible) return;

        TroopFootprint border = _troop.Border;
        BuildRings(border, TroopTuning.Active.FillEdgeFade, _outer, _inner);
        int count = _outer.Count;
        if (count < 3)
        {
            _outlineRenderer.enabled = false;
            return;
        }

        Color fill = new(armyColor.r, armyColor.g, armyColor.b, fillAlpha);
        Color clear = new(armyColor.r, armyColor.g, armyColor.b, 0f);
        _vertices.Clear();
        _colors.Clear();
        _triangles.Clear();

        // Solid middle: a fan from the centre to the inner ring (the shape is convex). Vertex 0 is the centre, 1..count
        // the inner ring, count+1..2count the outer ring.
        _vertices.Add(Local(border.Center));
        _colors.Add(fill);
        for (int i = 0; i < count; i++)
        {
            _vertices.Add(Local(_inner[i]));
            _colors.Add(fill);
        }
        for (int i = 0; i < count; i++)
        {
            _vertices.Add(Local(_outer[i]));
            _colors.Add(clear);
        }
        for (int i = 0; i < count; i++)
        {
            int next = (i + 1) % count;
            _triangles.Add(0);
            _triangles.Add(1 + i);
            _triangles.Add(1 + next);
            // Soft edge: the band from the inner ring (full) to the outer ring (clear).
            _triangles.Add(1 + i);
            _triangles.Add(1 + count + i);
            _triangles.Add(1 + count + next);
            _triangles.Add(1 + i);
            _triangles.Add(1 + count + next);
            _triangles.Add(1 + next);
        }

        _mesh.Clear();
        _mesh.SetVertices(_vertices);
        _mesh.SetColors(_colors);
        _mesh.SetTriangles(_triangles, 0);
        _mesh.RecalculateBounds();
    }

    // The border's edge (outer) and the same rounded box inset by the edge fade (inner), point for point, so the band
    // between them can be drawn as quads. Same corner order as TroopOutlinePolygon; the winding does not matter here.
    public static void BuildRings(in TroopFootprint border, float fade, List<Vector2> outer, List<Vector2> inner)
    {
        outer.Clear();
        inner.Clear();
        float rounding = border.Rounding;
        float smallest = Mathf.Min(border.HalfLength, border.HalfWidth) + rounding;
        fade = Mathf.Clamp(fade, 0f, smallest * MaxEdgeFadeShare);
        float innerRounding = Mathf.Max(0f, rounding - fade);
        float shrink = Mathf.Max(0f, fade - rounding);
        float innerHalfLength = Mathf.Max(0f, border.HalfLength - shrink);
        float innerHalfWidth = Mathf.Max(0f, border.HalfWidth - shrink);
        Vector2 axis = border.Axis;
        Vector2 facing = border.Facing;
        for (int corner = 0; corner < 4; corner++)
        {
            float alongSign = corner == 0 || corner == 3 ? 1f : -1f;
            float acrossSign = corner < 2 ? 1f : -1f;
            Vector2 core = border.Center + axis * (alongSign * border.HalfLength) + facing * (acrossSign * border.HalfWidth);
            Vector2 innerCore = border.Center + axis * (alongSign * innerHalfLength)
                + facing * (acrossSign * innerHalfWidth);
            for (int i = 0; i <= CornerSegments; i++)
            {
                float angle = (corner + i / (float)CornerSegments) * Mathf.PI * 0.5f;
                Vector2 direction = axis * Mathf.Cos(angle) + facing * Mathf.Sin(angle);
                outer.Add(core + direction * rounding);
                inner.Add(innerCore + direction * innerRounding);
            }
        }
    }

    Vector3 Local(Vector2 world) => transform.InverseTransformPoint(world);

    static void DestroyObject(Object target)
    {
        if (target == null) return;
        if (Application.isPlaying) Destroy(target);
        else DestroyImmediate(target);
    }
}
