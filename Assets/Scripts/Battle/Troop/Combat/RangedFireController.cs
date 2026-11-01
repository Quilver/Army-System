using System.Collections.Generic;
using UnityEngine;

// Ranged fire for one troop (spec §4.7). A troop with ranged soldiers fires while it is stationary, not engaged
// (no soldier engaged, Q-46), and not retreating or routed:
//   Reach (Decisions D-54): a shooter can hit an enemy soldier within its weapon range and within RangedFrontArcDeg of
//   the troop's facing, measured from the shooter (RangedRules.CanReach).
//   Target troop: the enemy troop not engaged in melee (Q-39) with the soldier nearest to a shooter that can reach it.
//   Each volley, every shooter fires one projectile at a target soldier it can reach, spreading fire across them.
//   After each volley the troop waits ActionWindowSeconds / FireRate (+/- 10% jitter) before the next (Q-43).
public sealed class RangedFireController
{
    readonly Troop _troop;
    readonly List<SoldierAgentState> _shooters = new();
    readonly List<Vector2> _shooterPositions = new();
    readonly List<(SoldierAgentState soldier, float distance)> _targets = new();
    readonly List<int> _aimed = new();
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
        BattleEvents.RaiseVolleyFired(_troop);
        _cooldown = RangedRules.FireInterval(fireRate, tuning.ActionWindowSeconds, tuning.FireRateJitterFraction, _troop.TroopRandom);
    }

    // Longest range among the soldiers that can shoot now (0 without one).
    public float MaxWeaponRange()
    {
        float range = 0f;
        foreach (var soldier in _troop.Soldiers)
            if (IsShooter(soldier, out RangedWeaponProfile weapon)) range = Mathf.Max(range, weapon.Range);
        return range;
    }

    // Every soldier that can shoot now, as (world x, world y, weapon range, fire rate). Drawn by TroopRangeMask.
    public void CollectReach(List<Vector4> reach)
    {
        reach.Clear();
        foreach (var soldier in _troop.Soldiers)
        {
            if (!IsShooter(soldier, out RangedWeaponProfile weapon)) continue;
            Vector2 position = _troop.WorldPosition(soldier);
            reach.Add(new Vector4(position.x, position.y, weapon.Range, weapon.FireRate));
        }
    }

    // A living, uncommitted soldier with a ranged weapon that has a range and a fire rate.
    static bool IsShooter(SoldierAgentState soldier, out RangedWeaponProfile weapon)
    {
        weapon = null;
        if (!soldier.IsAlive || soldier.IsCommitted || soldier.Character == null) return false;
        weapon = soldier.Character.RangedWeapon;
        return weapon != null && weapon.FireRate > 0 && weapon.Range > 0f;
    }

    bool CanFire() =>
        !_troop.IsRouted && !_troop.IsFaultSlipping && !_troop.IsEngaged && !_troop.IsMoving
        && _troop.PivotVelocity.sqrMagnitude < 0.01f && _troop.Roster.HasRangedSoldiers;

    void CollectShooters(out float range, out int fireRate)
    {
        _shooters.Clear();
        _shooterPositions.Clear();
        range = 0f;
        fireRate = 0;
        foreach (var soldier in _troop.Soldiers)
        {
            if (!IsShooter(soldier, out RangedWeaponProfile weapon)) continue;
            _shooters.Add(soldier);
            _shooterPositions.Add(_troop.WorldPosition(soldier));
            range = Mathf.Max(range, weapon.Range);
            if (fireRate == 0) fireRate = weapon.FireRate;
        }
    }

    // The enemy troop (not engaged in melee, Q-39) with the soldier nearest to any shooter that can reach it
    // (RangedRules.CanReach: within that shooter's range and within the front arc measured from the shooter, D-54).
    Troop FindTarget(IReadOnlyList<Troop> troops, float maxRange, float arcDegrees)
    {
        Vector2 pivot = _troop.transform.position;
        Vector2 facing = _troop.Facing;
        // Furthest shooter from the pivot, so whole troops out of reach are skipped cheaply.
        float shooterSpread = 0f;
        foreach (var position in _shooterPositions) shooterSpread = Mathf.Max(shooterSpread, (position - pivot).magnitude);

        Troop best = null;
        float bestDistanceSquared = float.PositiveInfinity;
        foreach (var other in troops)
        {
            if (other == null || other.TeamId == _troop.TeamId || other.LivingSoldierCount == 0) continue;
            if (other.IsEngaged) continue;
            if ((other.Footprint.ClosestPoint(pivot) - pivot).magnitude > maxRange + shooterSpread) continue;
            foreach (var enemy in other.Soldiers)
            {
                if (!enemy.IsAlive) continue;
                Vector2 enemyPosition = other.WorldPosition(enemy);
                for (int i = 0; i < _shooters.Count; i++)
                {
                    Vector2 offset = enemyPosition - _shooterPositions[i];
                    float distanceSquared = offset.sqrMagnitude;
                    if (distanceSquared >= bestDistanceSquared) continue;
                    if (!RangedRules.CanReach(offset, facing, arcDegrees, _shooters[i].Character.RangedWeapon.Range)) continue;
                    best = other;
                    bestDistanceSquared = distanceSquared;
                }
            }
        }
        return best;
    }

    // Each shooter fires one projectile at a target soldier it can reach. Fire is spread: a shooter picks the reachable
    // soldier with the fewest shots aimed at it so far this volley, the nearest on a tie. Shooters that cannot reach any
    // target soldier hold.
    void FireVolley(TroopProjectiles projectiles, TroopTuning tuning)
    {
        _targets.Clear();
        _aimed.Clear();
        foreach (var soldier in Target.Soldiers)
            if (soldier.IsAlive) _targets.Add((soldier, 0f));
        if (_targets.Count == 0) return;
        for (int t = 0; t < _targets.Count; t++) _aimed.Add(0);
        Vector2 facing = _troop.Facing;

        for (int i = 0; i < _shooters.Count; i++)
        {
            SoldierAgentState shooter = _shooters[i];
            RangedWeaponProfile weapon = shooter.Character.RangedWeapon;
            Vector2 from = _shooterPositions[i];
            int chosen = -1;
            float chosenDistance = float.PositiveInfinity;
            for (int t = 0; t < _targets.Count; t++)
            {
                Vector2 offset = Target.WorldPosition(_targets[t].soldier) - from;
                if (!RangedRules.CanReach(offset, facing, tuning.RangedFrontArcDeg, weapon.Range)) continue;
                float distance = offset.sqrMagnitude;
                if (chosen < 0 || _aimed[t] < _aimed[chosen] || (_aimed[t] == _aimed[chosen] && distance < chosenDistance))
                {
                    chosen = t;
                    chosenDistance = distance;
                }
            }
            if (chosen < 0) continue;
            _aimed[chosen]++;
            SoldierAgentState target = _targets[chosen].soldier;
            float hitChance = RangedRules.HitChance(weapon.Accuracy, shooter.Character.SkillFor(weapon.Class), tuning.BaseShotDC);
            projectiles.Launch(_troop, from, Target, target, RangedRules.RawDamage(weapon.Damage), hitChance, tuning.ProjectileSpeed);
        }
    }
}
