using UnityEngine;

// How a soldier's emission (the glowing mark) looks.
public enum SoldierGlow
{
    Healthy,   // the material's colour (also injured soldiers that are not fatigued)
    Fatigued,  // grey at a third of the material colour's luminosity (HP <= FatigueFraction x MaxHP)
    Dead       // black: no emission
}

// Sets a soldier's emission colour (the material's _Light colour, or _Color without it) through a property block, so the
// shared material is untouched: briefly brightened when the soldier clashes, grey and dimmed to a third when it is
// fatigued, black when it is dead (see TroopSoldierViews).
public sealed class ClashPulseView
{
    const float IntensityScale = 50 / 5f;
    public const float FatiguedBrightness = 1f / 3f;
    static readonly int LightColorId = Shader.PropertyToID("_Light");
    static readonly int ColorId = Shader.PropertyToID("_Color");
    readonly MaterialPropertyBlock _block = new();

    // progress 0 = full flash, 1 = back to normal.
    public void Apply(SpriteRenderer renderer, float progress, SoldierGlow glow = SoldierGlow.Healthy)
    {
        Material material = renderer.sharedMaterial;
        if (material == null) return;
        int colorProperty = material.HasProperty(LightColorId) ? LightColorId : ColorId;
        if (!material.HasProperty(colorProperty)) return;

        Color baseColor = material.GetColor(colorProperty);
        Color color = glow switch
        {
            SoldierGlow.Dead => Color.black,
            SoldierGlow.Fatigued => Grey(baseColor) * FatiguedBrightness,
            _ => baseColor
        };
        float intensity = glow == SoldierGlow.Dead ? 1f : Mathf.Lerp(IntensityScale, 1f, progress);
        color = new Color(color.r * intensity, color.g * intensity, color.b * intensity, baseColor.a);

        renderer.GetPropertyBlock(_block);
        _block.SetColor(colorProperty, color);
        renderer.SetPropertyBlock(_block);
    }

    // The colour's luminance as a grey (Rec. 709 weights), so an HDR glow keeps its brightness.
    public static Color Grey(Color color)
    {
        float luminance = 0.2126f * color.r + 0.7152f * color.g + 0.0722f * color.b;
        return new Color(luminance, luminance, luminance, color.a);
    }
}
