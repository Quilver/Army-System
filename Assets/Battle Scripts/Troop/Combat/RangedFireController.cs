using System.Collections.Generic;
using UnityEngine;

// Ranged fire for one troop (spec §4.7). A troop with ranged soldiers fires while it is stationary, not engaged
// (no soldier engaged, Q-46), and not retreating or routed:
//   Target troop: the nearest enemy troop whose footprint is within weapon range and in front of the line (within
//   RangedFrontArcDeg of the facing, measured from the pivot, Q-41), and that is not engaged in melee (Q-39).
//   Each volley, every ranged soldier shoots one projectile at one of the target troop's nearest soldiers.
//   After each volley the troop waits ActionWindowSeconds / FireRate (+/- 10% jitter) before the next (Q-43).
public sealed class RangedFireController
{
    readonly Troop _troop;
    readonly List<SoldierAgentState> _shooters = new();
    readonly List<(SoldierAgentState soldier, float distance)> _targets = new();
    float _cooldown;
    bool _primed;

    public RangedFireController(Troop troop)
    {
        _troop = troop;
    }

    public bool IsFiring { get; private set; }
    public Troop Target { get; private set; }
    public float Cooldown => _cooldown;
    public int Volleys { get; private set; }

    public void Reset()
    {
        _cooldown = 0f;
        _primed = false;
        Volleys = 0;
        Target = null;
        IsFiring = false;
    }

    public void Tick(IReadOnlyList<Troop> troops, TroopProjectiles projectiles, float deltaTime)
    {
        TroopTuning tuning = TroopTuning.Active;
        _cooldown = Mathf.Max(0f, _cooldown - deltaTime);
        Target = null;
        IsFiring = false;
        if (!CanFire()) return;

        CollectShooters(out float range, out int fireRate);
        if (_shooters.Count == 0) return;
        Target = FindTarget(troops, range, tuning.RangedFrontArcDeg);
        if (Target == null) return;
        IsFiring = true;

        // The first volley waits a random part of an interval, so troops do not fire in step.
        if (!_primed)
        {
            _primed = true;
            _cooldown = RangedRules.BaseInterval(fireRate, tuning.ActionWindowSeconds) * _troop.TroopRandom.Value();
        }
        if (_cooldown > 0f) return;

        FireVolley(projectiles, tuning);
        Volleys++;
        _cooldown = RangedRules.FireInterval(fireRate, tuning.ActionWindowSeconds, tuning.FireRateJitterFraction, _troop.TroopRandom);
    }

    bool CanFire() =>
        !_troop.IsRouted && !_troop.IsFaultSlipping && !_troop.IsEngaged && !_troop.IsMoving
        && _troop.PivotVelocity.sqrMagnitude < 0.01f && _troop.Roster.HasRangedSoldiers;

    void CollectShooters(out float range, out int fireRate)
    {
        _shooters.Clear();
        range = 0f;
        fireRate = 0;
        foreach (var soldier in _troop.Soldiers)
        {
            if (!soldier.IsAlive || soldier.IsCommitted || soldier.Profile == null) continue;
            WeaponProfile weapon = soldier.Profile.RangedWeapon;
            if (weapon == null || weapon.FireRate <= 0 || weapon.Range <= 0f) continue;
            _shooters.Add(soldier);
            range = Mathf.Max(range, weapon.Range);
            if (fireRate == 0) fireRate = weapon.FireRate;
        }
    }

    Troop FindTarget(IReadOnlyList<Troop> troops, float range, float arcDegrees)
    {
        Vector2 pivot = _troop.transform.position;
        Vector2 facing = _troop.Facing;
        Troop best = null;
        float bestDistance = float.PositiveInfinity;
        foreach (var other in troops)
        {
            if (other == null || other.TeamId == _troop.TeamId || other.LivingSoldierCount == 0) continue;
            if (other.IsEngaged) continue;
            Vector2 nearest = other.Footprint.ClosestPoint(pivot);
            Vector2 offset = nearest - pivot;
            float distance = offset.magnitude;
            if (distance > range || distance >= bestDistance) continue;
            if (distance > 1e-4f && Vector2.Angle(facing, offset) > arcDegrees) continue;
            best = other;
            bestDistance = distance;
        }
        return best;
    }

    void FireVolley(TroopProjectiles projectiles, TroopTuning tuning)
    {
        Vector2 pivot = _troop.transform.position;
        _targets.Clear();
        foreach (var soldier in Target.Soldiers)
            if (soldier.IsAlive) _targets.Add((soldier, (Target.WorldPosition(soldier) - pivot).sqrMagnitude));
        if (_targets.Count == 0) return;
        _targets.Sort((a, b) => a.distance.CompareTo(b.distance));
        int spread = Mathf.Min(_targets.Count, _shooters.Count);

        for (int i = 0; i < _shooters.Count; i++)
        {
            SoldierAgentState shooter = _shooters[i];
            WeaponProfile weapon = shooter.Profile.RangedWeapon;
            SoldierAgentState target = _targets[i % spread].soldier;
            Vector2 from = _troop.WorldPosition(shooter);
            Vector2 to = Target.WorldPosition(target);
            if (Vector2.Distance(from, to) > weapon.Range) continue;
            float hitChance = RangedRules.HitChance(weapon.Accuracy, shooter.Profile.SkillFor(weapon.Class), tuning.BaseShotDC);
            projectiles.Launch(_troop, from, Target, target, RangedRules.RawDamage(weapon.Damage), hitChance, tuning.ProjectileSpeed);
        }
    }
}
