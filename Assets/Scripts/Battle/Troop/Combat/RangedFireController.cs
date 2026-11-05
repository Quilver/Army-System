using System.Collections.Generic;
using UnityEngine;

// Ranged fire for one troop (spec §4.7). A troop with ranged soldiers fires while it is stationary, not engaged
// (no soldier engaged, Q-46), and not retreating or routed:
//   Reach (Decisions D-54): a shooter can hit an enemy soldier within its weapon range and within RangedFrontArcDeg of
//   the troop's facing, measured from the shooter (RangedRules.CanReach).
//   Target troop: the enemy troop not engaged in melee (Q-39) with the soldier nearest to a shooter that can reach it.
//   Each volley, every shooter fires one projectile at a target soldier it can reach, spreading fire across them.
//   After each volley the troop waits ActionWindowSeconds / FireRate (+/- 10% jitter) before the next (Q-43).
// Fire & Engage (SU-90): soldiers whose ranged weapon has the Fire & Engage rule also fire one volley when their troop
// enters an engagement (at the enemy it engages) and one when the troop retreats (spec §5.5) from that engagement, though
// it is engaged, moving or retreating. Both reset with each new engagement. These volleys need only weapon range (no
// front arc), may target an engaged troop, include soldiers that are charging, and do not use or reset the cooldown.
public sealed class RangedFireController
{
    readonly Troop _troop;
    readonly List<SoldierAgentState> _shooters = new();
    readonly List<Vector2> _shooterPositions = new();
    readonly List<(SoldierAgentState soldier, float distance)> _targets = new();
    readonly List<int> _aimed = new();
    float _cooldown;
    bool _primed;

    // Fire & Engage state (SU-90).
    bool _wasEngaged;
    bool _wasRetreating;
    bool _retreatVolleyUsed;
    Troop _engagedEnemy;

    public RangedFireController(Troop troop)
    {
        _troop = troop;
    }

    public bool IsFiring { get; private set; }
    public Troop Target { get; private set; }
    public float Cooldown => _cooldown;
    // Every volley fired, including Fire & Engage volleys.
    public int Volleys { get; private set; }
    // Fire & Engage volleys fired (SU-90).
    public int FireAndEngageVolleys { get; private set; }

    public void Reset()
    {
        _cooldown = 0f;
        _primed = false;
        Volleys = 0;
        FireAndEngageVolleys = 0;
        Target = null;
        IsFiring = false;
        _wasEngaged = false;
        _wasRetreating = false;
        _retreatVolleyUsed = false;
        _engagedEnemy = null;
    }

    public void Tick(IReadOnlyList<Troop> troops, TroopProjectiles projectiles, float deltaTime)
    {
        TroopTuning tuning = TroopTuning.Active;
        _cooldown = Mathf.Max(0f, _cooldown - deltaTime);
        Target = null;
        IsFiring = false;
        TickFireAndEngage(projectiles, tuning);
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

        FireVolley(Target, projectiles, tuning, useFrontArc: true);
        Volleys++;
        BattleEvents.RaiseVolleyFired(_troop);
        _cooldown = RangedRules.FireInterval(fireRate, tuning.ActionWindowSeconds, tuning.FireRateJitterFraction, _troop.TroopRandom);
    }

    // Fire & Engage (SU-90). A new engagement (not engaged last frame, engaged now) fires one volley at the enemy engaged
    // and makes the retreat volley available. A retreat (spec §5.5) that starts while engaged, or the frame after the
    // engagement ended, fires one volley at that enemy, once per engagement. A routed troop does neither.
    void TickFireAndEngage(TroopProjectiles projectiles, TroopTuning tuning)
    {
        bool engaged = _troop.IsEngaged && !_troop.IsRouted;
        bool retreating = _troop.IsRetreating && !_troop.IsRouted;
        Troop enemy = engaged ? _troop.EngagedEnemy : null;

        if (engaged && !_wasEngaged)
        {
            _retreatVolleyUsed = false;
            FireAndEngageVolley(enemy, projectiles, tuning);
        }
        if (retreating && !_wasRetreating && !_retreatVolleyUsed && (engaged || _wasEngaged))
        {
            _retreatVolleyUsed = true;
            FireAndEngageVolley(enemy != null ? enemy : _engagedEnemy, projectiles, tuning);
        }

        _engagedEnemy = engaged ? (enemy != null ? enemy : _engagedEnemy) : null;
        _wasEngaged = engaged;
        _wasRetreating = retreating;
    }

    void FireAndEngageVolley(Troop enemy, TroopProjectiles projectiles, TroopTuning tuning)
    {
        if (enemy == null || enemy.LivingSoldierCount == 0 || !_troop.Roster.HasRangedSoldiers) return;
        _shooters.Clear();
        _shooterPositions.Clear();
        foreach (var soldier in _troop.Soldiers)
        {
            if (!soldier.IsAlive || soldier.Character == null) continue;
            RangedWeaponProfile weapon = soldier.Character.RangedWeapon;
            if (weapon == null || weapon.Range <= 0f || !weapon.HasRule(WeaponSpecialRule.FireAndEngage)) continue;
            _shooters.Add(soldier);
            _shooterPositions.Add(_troop.WorldPosition(soldier));
        }
        if (_shooters.Count == 0) return;
        if (FireVolley(enemy, projectiles, tuning, useFrontArc: false) == 0) return;
        Volleys++;
        FireAndEngageVolleys++;
        BattleEvents.RaiseVolleyFired(_troop);
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

    // Each shooter fires one projectile at a soldier of targetTroop it can reach. Fire is spread: a shooter picks the
    // reachable soldier with the fewest shots aimed at it so far this volley, the nearest on a tie. Shooters that cannot
    // reach any target soldier hold. useFrontArc false (Fire & Engage) checks weapon range only. Returns the shots fired.
    int FireVolley(Troop targetTroop, TroopProjectiles projectiles, TroopTuning tuning, bool useFrontArc)
    {
        _targets.Clear();
        _aimed.Clear();
        if (targetTroop == null) return 0;
        foreach (var soldier in targetTroop.Soldiers)
            if (soldier.IsAlive) _targets.Add((soldier, 0f));
        if (_targets.Count == 0) return 0;
        int fired = 0;
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
                Vector2 offset = targetTroop.WorldPosition(_targets[t].soldier) - from;
                bool reachable = useFrontArc
                    ? RangedRules.CanReach(offset, facing, tuning.RangedFrontArcDeg, weapon.Range)
                    : offset.sqrMagnitude <= weapon.Range * weapon.Range;
                if (!reachable) continue;
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
            projectiles.Launch(_troop, from, targetTroop, target, RangedRules.RawDamage(weapon.Damage), hitChance,
                tuning.ProjectileSpeed, weapon);
            fired++;
        }
        return fired;
    }
}
