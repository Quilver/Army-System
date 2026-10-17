using UnityEngine;

public sealed class TroopVisualiser : MonoBehaviour
{
    const float PivotSize = 1f;
    const float DefaultRadiusAlpha = 0f;
    const float HoverRadiusAlpha = 15f / 255f;
    const float SelectedRadiusAlpha = 40f / 255f;
    public Sprite sprite;

    Troop _troop;
    bool _selected;
    bool _hovered;
    [SerializeField]
    SpriteRenderer _radiusRenderer;
    [SerializeField]
    SpriteRenderer _pivotRenderer;

    public void Initialize(Troop troop)
    {
        _troop = troop;
        _selected = troop.IsSelected;
        _hovered = troop.IsHovered;
        if (_radiusRenderer == null)
            _radiusRenderer = CreateRenderer("Troop Radius", SoldierSpriteLibrary.Circle, 10);
        if (_pivotRenderer == null)
            _pivotRenderer = CreateRenderer("Troop Pivot", sprite != null ? sprite : SoldierSpriteLibrary.Circle, 11);
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

    SpriteRenderer CreateRenderer(string objectName, Sprite rendererSprite, int sortingOrder)
    {
        var visual = new GameObject(objectName);
        visual.transform.SetParent(transform, false);
        var renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = rendererSprite;
        renderer.sharedMaterial = SoldierSpriteLibrary.ModelMaterial;
        renderer.sortingLayerName = "Terrain";
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    void UpdateVisuals()
    {
        if (_troop == null || _radiusRenderer == null || _pivotRenderer == null) return;

        Color armyColor = _troop.ArmyColor;
        float radiusAlpha = _selected ? SelectedRadiusAlpha : _hovered ? HoverRadiusAlpha : DefaultRadiusAlpha;
        _radiusRenderer.color = new Color(armyColor.r, armyColor.g, armyColor.b, radiusAlpha);
        _radiusRenderer.transform.localPosition = Vector3.zero;
        _radiusRenderer.transform.localScale = Vector3.one * (_troop.Radius * 2f);

        _pivotRenderer.color = _selected ? Color.Lerp(armyColor, Color.white, 0.55f) : armyColor;
        _pivotRenderer.transform.localPosition = Vector3.zero;
        _pivotRenderer.transform.localScale = Vector3.one * PivotSize;
    }
}
