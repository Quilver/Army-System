using UnityEngine;

// Battle Tuning: ranged fire, projectiles and composite troop ranks (spec §4.7, §7.2).
// Asset: Assets/Resources/Battle Tuning/Ranged Tuning.asset. Read through TroopTuning.Active.
[CreateAssetMenu(menuName = "Battle Tuning/Ranged Tuning")]
public sealed class RangedTuning : ScriptableObject
{
    public const string AssetName = "Ranged Tuning";

    [Header("Ranged (spec §4.7)")]
    [SerializeField, Min(1), Tooltip("Shot difficulty in the hit formula.")]
    int _baseShotDC = 10;
    [SerializeField, Range(0f, 1f), Tooltip("Random variation of the fire interval.")]
    float _fireRateJitterFraction = 0.1f;
    [SerializeField, Min(0.1f), Tooltip("Projectile speed in units/s.")]
    float _projectileSpeed = 20f;
    [SerializeField, Range(0f, 180f), Tooltip("A shooter can hit an enemy soldier within its weapon range and within this " +
        "angle of the troop's facing, measured from the shooter (Q-41, Decisions D-54).")]
    float _rangedFrontArcDeg = 45f;
    [SerializeField, Tooltip("Line of sight (SU-136): colliders on these layers block direct (not arced) shots. Default " +
        "Terrain, Impassable Terrain and Impassable; water blocks movement, not sight.")]
    LayerMask _sightBlockingLayers;
    // Whether _sightBlockingLayers has been given its default. Layer masks cannot be built in a field initialiser, so a
    // new section (or an asset saved before SU-136) takes the default in OnEnable; "Nothing" set by a designer is kept.
    [SerializeField, HideInInspector] bool _sightBlockingLayersSet;

    [Header("Composite troops (D-55)")]
    [SerializeField, Min(0f), Tooltip("Composite troops: how strongly ranged soldiers steer to the front rank (and melee " +
        "soldiers to the back) when not engaged, and the reverse when engaged (D-55). 0 turns it off.")]
    float _compositeRankGain = 0.8f;
    [SerializeField, Range(0f, 1f), Tooltip("Composite troops: depth of the front and back ranks, as a fraction of the " +
        "formation's half-depth from its centre line (D-55).")]
    float _compositeRankDepthFraction = 0.8f;

    public int BaseShotDC => _baseShotDC;
    public float FireRateJitterFraction => _fireRateJitterFraction;
    public float ProjectileSpeed => _projectileSpeed;
    public float RangedFrontArcDeg => _rangedFrontArcDeg;
    public LayerMask SightBlockingLayers => _sightBlockingLayers;
    public float CompositeRankGain => _compositeRankGain;
    public float CompositeRankDepthFraction => _compositeRankDepthFraction;

    // Default sight-blocking layers (SU-136). Layers missing from the project are left out.
    public static LayerMask DefaultSightBlockingLayers() => LayerMask.GetMask("Terrain", "Impassable Terrain", "Impassable");

    void OnEnable()
    {
        if (_sightBlockingLayersSet) return;
        _sightBlockingLayers = DefaultSightBlockingLayers();
        _sightBlockingLayersSet = true;
    }
}
