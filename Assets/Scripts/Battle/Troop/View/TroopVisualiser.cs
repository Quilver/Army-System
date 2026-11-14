using System.Collections.Generic;
using UnityEngine;

// Shows a troop's formation outline (Troop.Outline, see TroopOutline) and its pivot: a faint fill in the army colour,
// with no stroke along its border (SU-143). The fill is invisible until the troop is hovered or selected. The outline is
// the same shape the player can click inside to select the troop (TroopSelection).
public sealed class TroopVisualiser : MonoBehaviour
{
    const float PivotSize = 1f;
    const float DefaultFillAlpha = 0f;
    const float HoverFillAlpha = 15f / 255f;
    const float SelectedFillAlpha = 40f / 255f;
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

    readonly List<Vector2> _polygon = new();
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
        float fillAlpha = _selected ? SelectedFillAlpha : _hovered ? HoverFillAlpha : DefaultFillAlpha;

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

    void UpdateOutline(Color armyColor, float fillAlpha)
    {
        if (_outlineRenderer == null) return;
        bool visible = fillAlpha > 0f;
        if (_outlineRenderer.enabled != visible) _outlineRenderer.enabled = visible;
        if (!visible) return;

        TroopOutlinePolygon.Build(_troop.Outline, _polygon);
        int count = _polygon.Count;
        if (count < 3)
        {
            _outlineRenderer.enabled = false;
            return;
        }

        Color fill = new(armyColor.r, armyColor.g, armyColor.b, fillAlpha);
        _vertices.Clear();
        _colors.Clear();
        _triangles.Clear();

        // Fill: a fan from the footprint centre (the polygon is convex).
        Vector2 center = _troop.Outline.Center;
        _vertices.Add(Local(center));
        _colors.Add(fill);
        for (int i = 0; i < count; i++)
        {
            _vertices.Add(Local(_polygon[i]));
            _colors.Add(fill);
        }
        for (int i = 0; i < count; i++)
        {
            _triangles.Add(0);
            _triangles.Add(1 + i);
            _triangles.Add(1 + (i + 1) % count);
        }

        _mesh.Clear();
        _mesh.SetVertices(_vertices);
        _mesh.SetColors(_colors);
        _mesh.SetTriangles(_triangles, 0);
        _mesh.RecalculateBounds();
    }

    Vector3 Local(Vector2 world) => transform.InverseTransformPoint(world);

    static void DestroyObject(Object target)
    {
        if (target == null) return;
        if (Application.isPlaying) Destroy(target);
        else DestroyImmediate(target);
    }
}
