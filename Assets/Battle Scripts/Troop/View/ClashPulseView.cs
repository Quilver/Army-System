using UnityEngine;

// Briefly brightens a soldier's sprite colour when it clashes with an enemy, using a property block so the shared
// material is untouched.
public sealed class ClashPulseView
{
    const float IntensityScale = 50 / 5f;
    static readonly int LightColorId = Shader.PropertyToID("_Light");
    static readonly int ColorId = Shader.PropertyToID("_Color");
    readonly MaterialPropertyBlock _block = new();

    // progress 0 = full flash, 1 = back to normal.
    public void Apply(SpriteRenderer renderer, float progress)
    {
        Material material = renderer.sharedMaterial;
        if (material == null) return;
        int colorProperty = material.HasProperty(LightColorId) ? LightColorId : ColorId;
        if (!material.HasProperty(colorProperty)) return;

        renderer.GetPropertyBlock(_block);
        float intensityScale = Mathf.Lerp(IntensityScale, 1f, progress);
        _block.SetColor(colorProperty, material.GetColor(colorProperty) * intensityScale);
        renderer.SetPropertyBlock(_block);
    }
}