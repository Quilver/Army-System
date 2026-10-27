using UnityEngine;

// The troop's will to fight (spec §5). It holds Shock, the count of retreats and surrounded breaks, and the initial
// soldier count used by the rout threshold. Shock rises with applied hits and decays continuously; there is no fault
// pressure, clash cooldown, break cooldown or charge lock. Whether the troop breaks is decided by Troop.EvaluateBreak.
// Disable to stop new shock and breaks; a retreat already under way finishes.
[DisallowMultipleComponent]
public sealed class TroopMorale : MonoBehaviour
{
    const int DefaultBaseMorale = CombatStatLimits.Default;  // used only when the troop has no profile

    Troop _troop;
    float _shock;
    int _initialSoldiers;
    int _retreatCount;
    int _surroundedBreakCount;
    Vector2 _lastAttackerPivotPosition;
    bool _hasAttacker;

    TroopTuning Tuning => TroopTuning.Active;

    public int BaseMorale => _troop != null && _troop.Profile != null ? _troop.Profile.BaseMorale : DefaultBaseMorale;
    public int InitialSoldierCount => _initialSoldiers;
    public int RetreatCount => _retreatCount;
    public int SurroundedBreakCount => _surroundedBreakCount;
    public float Shock => _shock;

    // EffectiveMorale = BaseMorale x living soldiers minus the break penalties (spec §5.1).
    public float EffectiveMorale => MoraleRules.EffectiveMorale(
        BaseMorale,
        _troop != null ? _troop.LivingSoldierCount : 0,
        _retreatCount,
        _surroundedBreakCount,
        Tuning.RetreatMoralePenalty,
        Tuning.SurroundedMoralePenalty);

    // BaseMorale x the soldiers the troop deployed with. The rout threshold is a fraction of this.
    public float InitialEffectiveMorale => MoraleRules.InitialEffectiveMorale(BaseMorale, _initialSoldiers);

    // Shock lost per second at the current EffectiveMorale.
    public float DecayPerSecond => MoraleRules.DecayPerSecond(EffectiveMorale, Tuning.ShockDecayFractionPerWindow, Tuning.ActionWindowSeconds);

    public Vector2 LastAttackerPivotPosition => _lastAttackerPivotPosition;
    public bool HasAttacker => _hasAttacker;

    internal void Bind(Troop troop) => _troop = troop;

    // Called once when the soldiers are spawned. Resets the battle record.
    internal void CaptureInitialSoldiers(int count)
    {
        _initialSoldiers = Mathf.Max(0, count);
        _shock = 0f;
        _retreatCount = 0;
        _surroundedBreakCount = 0;
        _hasAttacker = false;
    }

    // Shock decays at all times, including in melee, and is clamped at zero.
    internal void Tick(float deltaTime)
    {
        _shock = MoraleRules.DecayShock(
            _shock,
            EffectiveMorale,
            Tuning.ShockDecayFractionPerWindow,
            Tuning.ActionWindowSeconds,
            deltaTime);
    }

    // Adds the applied points (bruise plus bleed, overflow excluded). Flanked hits are multiplied (spec §5.2).
    internal void AddShock(float points, bool flanked, Vector2 attackerPivotPosition)
    {
        if (!enabled || points <= 0f) return;
        _shock += points * (flanked ? Tuning.FlankShockMultiplier : 1f);
        _lastAttackerPivotPosition = attackerPivotPosition;
        _hasAttacker = true;
    }

    // The case that would apply if the troop broke now (for the Inspector), or None when it would not break.
    public BreakCase Preview(bool surrounded) =>
        MoraleRules.Evaluate(_shock, EffectiveMorale, InitialEffectiveMorale, surrounded, Tuning.RoutMoraleThresholdFraction);

    // The case that applies if the troop would break with this Shock, ignoring whether it is breaking now.
    public BreakCase CaseIfBroken(bool surrounded) =>
        MoraleRules.Evaluate(float.PositiveInfinity, EffectiveMorale, InitialEffectiveMorale, surrounded,
            Tuning.RoutMoraleThresholdFraction);

    // A normal retreat (case B): shock resets and EffectiveMorale drops by the retreat penalty.
    internal void RecordRetreat()
    {
        _shock = 0f;
        _retreatCount++;
    }

    // A surrounded break (case C): shock resets and EffectiveMorale drops by the surrounded penalty.
    internal void RecordSurroundedBreak()
    {
        _shock = 0f;
        _surroundedBreakCount++;
    }

    // Test hook: set Shock directly.
    internal void SetShockForTests(float shock) => _shock = Mathf.Max(0f, shock);
}
