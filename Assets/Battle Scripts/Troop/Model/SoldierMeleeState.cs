using System;
using UnityEngine;

// Melee attack bookkeeping for one soldier.
//   Accrual:    while engaged in a clash, attacks build up at Aggression / PerMeleeTime per second. The expected number of
//               attacks per PerMeleeTime seconds is therefore Aggression, regardless of Discipline.
//   Discipline: each completed attack is either ready at once (independent) or held until the troop's next beat. The
//               chance of holding is Discipline / MaxDiscipline. Held attacks are released together on the beat.
//   Spending:   a soldier in contact with an enemy spends one ready attack to start a clash. Pending attacks are capped
//               at MaxPendingAttacks, so attacks that are never used are lost rather than banked indefinitely.
[Serializable]
public sealed class SoldierMeleeState
{
    const int MaxPendingAttacks = 4;

    [SerializeField] float _progress;
    [SerializeField] int _readyAttacks;
    [SerializeField] int _heldAttacks;
    [SerializeField] bool _engaged;

    // Runtime-only lunge toward the opponent of the attack that started a clash (not serialized).
    SoldierAgentState _lungeTarget;
    Troop _lungeTroop;
    float _lungeRemaining;

    public int ReadyAttacks => _readyAttacks;
    public int HeldAttacks => _heldAttacks;
    public SoldierAgentState LungeTarget => _lungeTarget;
    public Troop LungeTroop => _lungeTroop;
    public bool IsLunging => _lungeTarget != null && _lungeTroop != null && _lungeRemaining > 0f;

    public void BeginLunge(SoldierAgentState target, Troop targetTroop, float duration)
    {
        _lungeTarget = target;
        _lungeTroop = targetTroop;
        _lungeRemaining = duration;
    }

    // Called during clash resolution. Only engaged soldiers accrue attacks in Tick.
    public void MarkEngaged() => _engaged = true;

    public bool TrySpendAttack()
    {
        if (_readyAttacks <= 0) return false;
        _readyAttacks--;
        return true;
    }

    // Advances the attack state by one frame. Call once per frame after clashes are resolved.
    // attacksPerSecond: Aggression / PerMeleeTime. holdChance: Discipline / MaxDiscipline in [0, 1].
    public void Tick(float attacksPerSecond, float holdChance, bool troopBeat, float deltaTime)
    {
        if (_lungeRemaining > 0f) _lungeRemaining -= deltaTime;

        if (troopBeat && _heldAttacks > 0)
        {
            _readyAttacks += _heldAttacks;
            _heldAttacks = 0;
        }

        if (_engaged && attacksPerSecond > 0f)
        {
            _progress += attacksPerSecond * deltaTime;
            while (_progress >= 1f && _readyAttacks + _heldAttacks < MaxPendingAttacks)
            {
                _progress -= 1f;
                if (UnityEngine.Random.value < holdChance) _heldAttacks++;
                else _readyAttacks++;
            }
            _progress = Mathf.Min(_progress, 1f);
        }

        _engaged = false;
    }
}
