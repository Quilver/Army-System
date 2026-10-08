using UnityEngine;

public sealed class FlockVisualiser : MonoBehaviour
{
    const float PivotSize = 1f;
    const float RadiusAlpha = 0.12f;
    public Sprite sprite;

    Flock _flock;
    bool _selected;
    [SerializeField]
    SpriteRenderer _radiusRenderer;
    [SerializeField]
    SpriteRenderer _pivotRenderer;

    public void Initialize(Flock flock)
    {
        _flock = flock;
        _selected = flock.IsSelected;
        if (_radiusRenderer == null)
            _radiusRenderer = CreateRenderer("Flock Radius", BoidSpriteLibrary.Circle, 10);
        if (_pivotRenderer == null)
            _pivotRenderer = CreateRenderer("Flock Pivot", sprite != null ? sprite : BoidSpriteLibrary.Circle, 11);
        UpdateVisuals();
    }

    public void SetSelected(bool selected)
    {
        _selected = selected;
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
        renderer.sharedMaterial = BoidSpriteLibrary.ModelMaterial;
        renderer.sortingLayerName = "Terrain";
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    void UpdateVisuals()
    {
        if (_flock == null || _radiusRenderer == null || _pivotRenderer == null) return;

        Color armyColor = _flock.ArmyColor;
        _radiusRenderer.color = new Color(armyColor.r, armyColor.g, armyColor.b, RadiusAlpha);
        _radiusRenderer.transform.localPosition = Vector3.zero;
        _radiusRenderer.transform.localScale = Vector3.one * (_flock.Radius * 2f);

        _pivotRenderer.color = _selected ? Color.Lerp(armyColor, Color.white, 0.55f) : armyColor;
        _pivotRenderer.transform.localPosition = Vector3.zero;
        _pivotRenderer.transform.localScale = Vector3.one * PivotSize;
    }
}
