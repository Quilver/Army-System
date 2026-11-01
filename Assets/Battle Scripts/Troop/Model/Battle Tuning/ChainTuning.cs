using UnityEngine;

// Battle Tuning: chains of linked troops and their link springs (spec §3.4, §7.2).
// Asset: Assets/Resources/Battle Tuning/Chain Tuning.asset. Read through TroopTuning.Active.
[CreateAssetMenu(menuName = "Battle Tuning/Chain Tuning")]
public sealed class ChainTuning : ScriptableObject
{
    public const string AssetName = "Chain Tuning";

    [Header("Chains (spec §3.4)")]
    [SerializeField, Min(0.1f), Tooltip("Minimum line length of a linked troop, x L_nat.")]
    float _chainMinStretchScale = 0.7f;
    [SerializeField, Min(1f), Tooltip("Maximum line length of a linked troop, x L_nat. Longer breaks the link.")]
    float _chainMaxStretchScale = 3f;
    [SerializeField, Range(0f, 180f), Tooltip("Minimum inner hinge angle at a joint. Sharper breaks the link.")]
    float _chainMinInnerAngleDeg = 65f;
    [SerializeField, Min(0f), Tooltip("Endpoint-to-endpoint attach distance, x L_nat.")]
    float _attachScale = 0.25f;
    [SerializeField, Min(0f), Tooltip("ChainSuppressed ends this long after the troop leaves every attach radius (Q-22).")]
    float _chainSuppressCooldownSeconds = 3f;
    [SerializeField, Min(1f), Tooltip("Stretch (x L_nat) that makes an unengaged neighbour retreat with its chain.")]
    float _chainRetreatStretchThreshold = 1.4f;
    [SerializeField, Range(0f, 180f), Tooltip("Hinge bend (180 - inner angle) that makes an unengaged neighbour retreat. Q-13.")]
    float _chainRetreatHingeBendDeg = 30f;
    [SerializeField, Range(0f, 1f), Tooltip("Fraction of the retreat distance lost per hop. Provisional (Q-21).")]
    float _chainRetreatFalloff = 0.3f;
    [SerializeField, Min(0.01f), Tooltip("Speed at which a stretched endpoint relaxes to L_nat after its link breaks.")]
    float _chainEndpointRelaxSpeed = 2f;

    [Header("Link spring")]
    [SerializeField, Min(0f), Tooltip("Link spring: gap allowed before the spring pulls.")]
    float _linkSlack = 0.1f;
    [SerializeField, Min(0f), Tooltip("Link spring stiffness.")]
    float _linkStiffness = 5f;
    [SerializeField, Min(0f), Tooltip("Link spring maximum force.")]
    float _linkMaxForce = 10.4f;

    public float ChainMinStretchScale => _chainMinStretchScale;
    public float ChainMaxStretchScale => _chainMaxStretchScale;
    public float ChainMinInnerAngleDeg => _chainMinInnerAngleDeg;
    public float AttachScale => _attachScale;
    public float ChainSuppressCooldownSeconds => _chainSuppressCooldownSeconds;
    public float ChainRetreatStretchThreshold => _chainRetreatStretchThreshold;
    public float ChainRetreatHingeBendDeg => _chainRetreatHingeBendDeg;
    public float ChainRetreatFalloff => _chainRetreatFalloff;
    public float ChainEndpointRelaxSpeed => _chainEndpointRelaxSpeed;
    public float LinkSlack => _linkSlack;
    public float LinkStiffness => _linkStiffness;
    public float LinkMaxForce => _linkMaxForce;
}
