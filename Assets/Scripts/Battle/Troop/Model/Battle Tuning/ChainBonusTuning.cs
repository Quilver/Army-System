using UnityEngine;

// Battle Tuning: what a troop gains from being linked in a chain (SU-152). Kept apart from Chain Tuning (the chain's
// geometry) so it can be experimented with on its own.
// Asset: Assets/Resources/Battle Tuning/Chain Bonus Tuning.asset. Read through TroopTuning.Active.
//   Per active link (0, 1 or 2; pending order links do not count): + Defence Per Link to each soldier's TotalDefence and
//   + Morale Per Link to the troop's BaseMorale in EffectiveMorale (so a chain end gets one link's bonus and a troop in
//   the middle two). The initial EffectiveMorale, and so the rout threshold, is not raised.
[CreateAssetMenu(menuName = "Battle Tuning/Chain Bonus Tuning")]
public sealed class ChainBonusTuning : ScriptableObject
{
    public const string AssetName = "Chain Bonus Tuning";

    [Header("Chain bonus (SU-152)")]
    [SerializeField, Tooltip("On: a troop gets the bonuses below for each active link of its chain.")]
    bool _chainBonus = true;
    [SerializeField, Tooltip("Added to every soldier's TotalDefence (Defence + armour) per active link. Soldiers with no " +
        "armour roll every point anyway, so it does not change their damage.")]
    int _defencePerLink = 1;
    [SerializeField, Tooltip("Added to the troop's BaseMorale (per soldier) in EffectiveMorale per active link. Losing a link " +
        "loses the bonus at once, which can break a troop whose Shock is above the lower EffectiveMorale.")]
    int _moralePerLink = 1;

    public bool ChainBonus => _chainBonus;
    public int DefencePerLink => _defencePerLink;
    public int MoralePerLink => _moralePerLink;

    // Defence and BaseMorale bonuses for a troop with this many active links (0 when the bonus is off).
    public int DefenceBonus(int activeLinks) => _chainBonus ? Mathf.Max(0, activeLinks) * _defencePerLink : 0;
    public int MoraleBonus(int activeLinks) => _chainBonus ? Mathf.Max(0, activeLinks) * _moralePerLink : 0;
}
