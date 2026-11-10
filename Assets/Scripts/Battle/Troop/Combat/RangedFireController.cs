using System.Collections.Generic;
using UnityEngine;

// Ranged fire for one troop (spec §4.7). A troop with ranged soldiers fires while it is stationary, not engaged
// (no soldier engaged, Q-46), and not retreating or routed:
//   Reach (Decisions D-54): a shooter can hit an enemy soldier within its weapon range and within RangedFrontArcDeg of
//   the troop's facing, measured from the shooter (RangedRules.CanReach).
//   Target troop: the enemy troop not engaged in melee (Q-39) with the soldier nearest to a shooter that can reach it.
//   Volley weapons (RangedWeaponProfile.Volley, SU-105): each volley, every soldier with one fires one projectile at a
//   target soldier it can reach, spreading fire across them. After each volley the troop waits
//   ActionWindowSeconds / FireRate (+/- 10% jitter) before the next (Q-43).
//   Other weapons (the default): each soldier shoots on its own clock, one shot every ActionWindowSeconds / FireRate
//   (+/- 10%), its first shot after a random part of that, so the troop's fire is spread out rather than in step. Each
//   shot goes to the reachable soldier with the fewest of this troop's projectiles in flight at it.
// Fire & Engage (SU-90): soldiers whose ranged weapon has the Fire & Engage rule also fire one volley when their troop
// enters an engagement (at the enemy it engages) and one when the troop retreats (spec §5.5) from that engagement, though
// it is engaged, moving or retreating. Both reset with each new engagement. These volleys need only weapon range (no
// front arc), may target an engaged troop, include soldiers that are charging, and do not use or reset the cooldown.
// Line of sight (SU-136): a shooter whose weapon is not ArcedShot (direct fire) shoots only at a soldier it can see: the
// straight line from it to that soldier crosses no collider on TroopTuning.SightBlockingLayers and passes within a
// soldier radius of no living soldier of a third troop (any troop, friend or enemy, other than the shooter's and the
// target's). A direct shooter with no visible soldier in reach holds its shot. The target troop is the nearest one the
// troop can see; a troop no shooter can see is skipped. Sight is checked when a shot is fired, not in flight. Arced
// weapons ignore all of this. Applies to every shot, Fire & Engage volleys included.
public sealed class RangedFireController
{
    // A troop no shooter could see is not tried again as the target for this long (SU-136), to keep sight checks cheap.
    const float HiddenRecheckSeconds = 0.25f;

    readonly Troop _troop;
    readonly List<SoldierAgentState> _shooters = new();
    readonly List<Vector2> _shooterPositions = new();
    readonly List<(SoldierAgentState soldier, float distance)> _targets = new();
    readonly List<int> _aimed = new();
    readonly List<bool> _ready = new();
    // Each soldier's own shot clock, for weapons that do not fire volleys (SU-105).
    readonly Dictionary<SoldierAgentState, float> _shotCooldowns = new();
    // Line of sight (SU-136).
    readonly List<Vector2> _blockers = new();
    readonly List<(Troop troop, float distanceSquared, int order)> _candidates = new();
    readonly List<(int index, float distanceSquared, Vector2 position)> _options = new();
    readonly Dictionary<Troop, float> _hiddenTroops = new();
    readonly List<Troop> _hiddenKeys = new();
    readonly RaycastHit2D[] _terrainHits = new RaycastHit2D[1];
    readonly System.Comparison<(int index, float distanceSquared, Vector2 position)> _byPreference;
    IReadOnlyList<Troop> _troops;
    ContactFilter2D _sightFilter;
    bool _terrainBlocksSight;
    // The target troop _blockers was gathered for, with the current shooters; null when it must be gathered again.
    Troop _blockersFor;
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
        _byPreference = ComparePreference;
    }

    public bool IsFiring { get; private set; }
    public Troop Target { get; private set; }
    public float Cooldown => _cooldown;
    // Every volley fired, including Fire & Engage volleys.
    public int Volleys { get; private set; }
    // Fire & Engage volleys fired (SU-90).
    public int FireAndEngageVolleys { get; private set; }
    // Shots fired by soldiers shooting on their own clocks (weapons without Volley, SU-105).
    public int SingleShots { get; private set; }

    public void Reset()
    {
        _cooldown = 0f;
        _primed = false;
        Volleys = 0;
        FireAndEngageVolleys = 0;
        SingleShots = 0;
        _shotCooldowns.Clear();
        Target = null;
        IsFiring = false;
        _wasEngaged = false;
        _wasRetreating = false;
        _retreatVolleyUsed = false;
        _engagedEnemy = null;
        _hiddenTroops.Clear();
        _blockersFor = null;
    }

    public void Tick(IReadOnlyList<Troop> troops, TroopProjectiles projectiles, float deltaTime)
    {
        TroopTuning tuning = TroopTuning.Active;
        _cooldown = Mathf.Max(0f, _cooldown - deltaTime);
        TickShotCooldowns(deltaTime);
        TickHiddenTroops(deltaTime);
        PrepareSight(troops, tuning);
        Target = null;
        IsFiring = false;
        TickFireAndEngage(projectiles, tuning);
        if (!CanFire()) return;

        CollectShooters(out float range, out int fireRate);
        if (_shooters.Count == 0) return;
        _blockersFor = null;
        Target = FindTarget(troops, range, tuning.RangedFrontArcDeg);
        if (Target == null) return;
        IsFiring = true;

        FireSingleShots(Target, projectiles, tuning);
        if (fireRate <= 0) return;

        // The first volley waits a random part of an interval, so troops do not fire in step.
        if (!_primed)
        {
            _primed = true;
            _cooldown = RangedRules.BaseInterval(fireRate, tuning.ActionWindowSeconds) * _troop.TroopRandom.Value();
        }
        if (_cooldown > 0f) return;

        if (!MarkShooters(VolleyWeapon)) return;
        // SU-136: a volley shooter that sees no target soldier holds; if none fires, the volley waits for a clear line.
        if (FireVolley(Target, projectiles, tuning, useFrontArc: true) == 0) return;
        Volleys++;
        BattleEvents.RaiseVolleyFired(_troop);
        _cooldown = RangedRules.FireInterval(fireRate, tuning.ActionWindowSeconds, tuning.FireRateJitterFraction, _troop.TroopRandom);
    }

    static bool VolleyWeapon(RangedWeaponProfile weapon) => weapon.Volley;

    // Marks the shooters whose weapon passes the test as ready to fire. Returns false when none does.
    bool MarkShooters(System.Func<RangedWeaponProfile, bool> test)
    {
        _ready.Clear();
        bool any = false;
        foreach (var shooter in _shooters)
        {
            bool ready = test(shooter.Character.RangedWeapon);
            _ready.Add(ready);
            any |= ready;
        }
        return any;
    }

    // Soldiers whose weapon does not fire volleys (SU-105) count down their own clocks, at all times.
    void TickShotCooldowns(float deltaTime)
    {
        if (_shotCooldowns.Count == 0 || deltaTime <= 0f) return;
        _shotKeys.Clear();
        _shotKeys.AddRange(_shotCooldowns.Keys);
        foreach (var soldier in _shotKeys)
        {
            if (!soldier.IsAlive) { _shotCooldowns.Remove(soldier); continue; }
            _shotCooldowns[soldier] = Mathf.Max(0f, _shotCooldowns[soldier] - deltaTime);
        }
    }

    readonly List<SoldierAgentState> _shotKeys = new();

    // Each soldier without a volley weapon fires when its own clock runs out (SU-105). Its first shot waits a random part
    // of its interval, so the troop's shots are spread out. Shots go where the fewest of this troop's projectiles are
    // already flying.
    void FireSingleShots(Troop target, TroopProjectiles projectiles, TroopTuning tuning)
    {
        ITroopRandom random = _troop.TroopRandom;
        _ready.Clear();
        bool any = false;
        foreach (var shooter in _shooters)
        {
            RangedWeaponProfile weapon = shooter.Character.RangedWeapon;
            bool ready = false;
            if (!weapon.Volley)
            {
                if (!_shotCooldowns.TryGetValue(shooter, out float remaining))
                {
                    remaining = RangedRules.BaseInterval(weapon.FireRate, tuning.ActionWindowSeconds) * random.Value();
                    _shotCooldowns[shooter] = remaining;
                }
                ready = remaining <= 0f;
            }
            _ready.Add(ready);
            any |= ready;
        }
        if (!any) return;

        int fired = FireVolley(target, projectiles, tuning, useFrontArc: true, countInFlight: projectiles,
            onFired: shooter => _shotCooldowns[shooter] = RangedRules.FireInterval(shooter.Character.RangedWeapon.FireRate,
                tuning.ActionWindowSeconds, tuning.FireRateJitterFraction, random));
        if (fired == 0) return;
        SingleShots += fired;
        BattleEvents.RaiseVolleyFired(_troop);
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
        _blockersFor = null;
        MarkShooters(_ => true);
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
            // The volley clock runs at the first volley weapon's fire rate (0 when no soldier has one).
            if (fireRate == 0 && weapon.Volley) fireRate = weapon.FireRate;
        }
    }

    // The enemy troop (not engaged in melee, Q-39) with the soldier nearest to any shooter that can reach it
    // (RangedRules.CanReach: within that shooter's range and within the front arc measured from the shooter, D-54), among
    // the troops the troop can see (SU-136): troops are tried nearest first, and one that no shooter can see a reachable
    // soldier of is skipped (and not tried again for HiddenRecheckSeconds).
    Troop FindTarget(IReadOnlyList<Troop> troops, float maxRange, float arcDegrees)
    {
        Vector2 pivot = _troop.transform.position;
        Vector2 facing = _troop.Facing;
        // Furthest shooter from the pivot, so whole troops out of reach are skipped cheaply.
        float shooterSpread = 0f;
        foreach (var position in _shooterPositions) shooterSpread = Mathf.Max(shooterSpread, (position - pivot).magnitude);

        _candidates.Clear();
        for (int order = 0; order < troops.Count; order++)
        {
            Troop other = troops[order];
            if (other == null || other.TeamId == _troop.TeamId || other.LivingSoldierCount == 0) continue;
            if (other.IsEngaged || _hiddenTroops.ContainsKey(other)) continue;
            if ((other.Footprint.ClosestPoint(pivot) - pivot).magnitude > maxRange + shooterSpread) continue;
            float nearest = float.PositiveInfinity;
            foreach (var enemy in other.Soldiers)
            {
                if (!enemy.IsAlive) continue;
                Vector2 enemyPosition = other.WorldPosition(enemy);
                for (int i = 0; i < _shooters.Count; i++)
                {
                    Vector2 offset = enemyPosition - _shooterPositions[i];
                    float distanceSquared = offset.sqrMagnitude;
                    if (distanceSquared >= nearest) continue;
                    if (!RangedRules.CanReach(offset, facing, arcDegrees, _shooters[i].Character.RangedWeapon.Range)) continue;
                    nearest = distanceSquared;
                }
            }
            if (!float.IsPositiveInfinity(nearest)) _candidates.Add((other, nearest, order));
        }
        if (_candidates.Count > 1) _candidates.Sort(CompareCandidates);
        foreach (var candidate in _candidates)
        {
            if (CanSeeAnySoldier(candidate.troop, facing, arcDegrees)) return candidate.troop;
            _hiddenTroops[candidate.troop] = HiddenRecheckSeconds;
        }
        return null;
    }

    static int CompareCandidates((Troop troop, float distanceSquared, int order) a, (Troop troop, float distanceSquared, int order) b)
    {
        int byDistance = a.distanceSquared.CompareTo(b.distanceSquared);
        return byDistance != 0 ? byDistance : a.order.CompareTo(b.order);
    }

    // Whether any shooter can reach and see (SU-136) a living soldier of targetTroop.
    bool CanSeeAnySoldier(Troop targetTroop, Vector2 facing, float arcDegrees)
    {
        foreach (var enemy in targetTroop.Soldiers)
        {
            if (!enemy.IsAlive) continue;
            Vector2 enemyPosition = targetTroop.WorldPosition(enemy);
            for (int i = 0; i < _shooters.Count; i++)
            {
                Vector2 offset = enemyPosition - _shooterPositions[i];
                if (!RangedRules.CanReach(offset, facing, arcDegrees, _shooters[i].Character.RangedWeapon.Range)) continue;
                if (CanSee(i, targetTroop, enemyPosition)) return true;
            }
        }
        return false;
    }

    // --- Line of sight (SU-136) ---

    void PrepareSight(IReadOnlyList<Troop> troops, TroopTuning tuning)
    {
        _troops = troops;
        _blockersFor = null;
        LayerMask mask = tuning.SightBlockingLayers;
        _terrainBlocksSight = mask.value != 0;
        _sightFilter = new ContactFilter2D { useLayerMask = true, layerMask = mask, useTriggers = false };
    }

    void TickHiddenTroops(float deltaTime)
    {
        if (_hiddenTroops.Count == 0) return;
        _hiddenKeys.Clear();
        _hiddenKeys.AddRange(_hiddenTroops.Keys);
        foreach (var troop in _hiddenKeys)
        {
            float left = _hiddenTroops[troop] - Mathf.Max(0f, deltaTime);
            if (troop == null || left <= 0f) _hiddenTroops.Remove(troop);
            else _hiddenTroops[troop] = left;
        }
    }

    // Whether shooter (an index into _shooters) can see a soldier of targetTroop at targetPosition. Arced weapons always
    // can. Direct fire needs a line clear of third-troop soldiers (within a soldier radius) and of sight-blocking terrain.
    bool CanSee(int shooter, Troop targetTroop, Vector2 targetPosition)
    {
        if (_shooters[shooter].Character.RangedWeapon.ArcedShot) return true;
        Vector2 from = _shooterPositions[shooter];
        GatherBlockers(targetTroop);
        if (RangedRules.LineBlockedBySoldiers(from, targetPosition, _blockers, TroopConstants.SoldierRadius)) return false;
        return !_terrainBlocksSight || Physics2D.Linecast(from, targetPosition, _sightFilter, _terrainHits) == 0;
    }

    // The living soldiers of every troop but this one and targetTroop that could stand in a line of fire: those inside the
    // box around the current shooters and targetTroop's soldiers, widened by a soldier radius. Gathered once per target
    // troop and shooter set.
    void GatherBlockers(Troop targetTroop)
    {
        if (_blockersFor == targetTroop) return;
        _blockersFor = targetTroop;
        _blockers.Clear();
        if (_troops == null) return;
        Vector2 min = new(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new(float.NegativeInfinity, float.NegativeInfinity);
        foreach (var position in _shooterPositions)
        {
            min = Vector2.Min(min, position);
            max = Vector2.Max(max, position);
        }
        foreach (var soldier in targetTroop.Soldiers)
        {
            if (!soldier.IsAlive) continue;
            Vector2 position = targetTroop.WorldPosition(soldier);
            min = Vector2.Min(min, position);
            max = Vector2.Max(max, position);
        }
        Vector2 margin = Vector2.one * TroopConstants.SoldierRadius;
        min -= margin;
        max += margin;
        foreach (var other in _troops)
        {
            if (other == null || other == _troop || other == targetTroop || other.LivingSoldierCount == 0) continue;
            foreach (var soldier in other.Soldiers)
            {
                if (!soldier.IsAlive) continue;
                Vector2 position = other.WorldPosition(soldier);
                if (position.x < min.x || position.y < min.y || position.x > max.x || position.y > max.y) continue;
                _blockers.Add(position);
            }
        }
    }

    // Shot preference: fewest shots aimed at the soldier, then the nearest, then list order.
    int ComparePreference((int index, float distanceSquared, Vector2 position) a, (int index, float distanceSquared, Vector2 position) b)
    {
        int byAimed = _aimed[a.index].CompareTo(_aimed[b.index]);
        if (byAimed != 0) return byAimed;
        int byDistance = a.distanceSquared.CompareTo(b.distanceSquared);
        return byDistance != 0 ? byDistance : a.index.CompareTo(b.index);
    }

    // Each ready shooter (_ready, set by MarkShooters or FireSingleShots) fires one projectile at a soldier of targetTroop
    // it can reach. Fire is spread: a shooter picks the reachable soldier with the fewest shots aimed at it so far (this
    // volley, plus this troop's projectiles already in flight when countInFlight is given), the nearest on a tie; a
    // direct-fire shooter takes the first of those it can see (SU-136). Shooters that cannot reach and see any target
    // soldier hold. useFrontArc false (Fire & Engage) checks weapon range only.
    // onFired is called for each shooter that fired. Returns the shots fired.
    int FireVolley(Troop targetTroop, TroopProjectiles projectiles, TroopTuning tuning, bool useFrontArc,
        TroopProjectiles countInFlight = null, System.Action<SoldierAgentState> onFired = null)
    {
        _targets.Clear();
        _aimed.Clear();
        if (targetTroop == null) return 0;
        foreach (var soldier in targetTroop.Soldiers)
            if (soldier.IsAlive) _targets.Add((soldier, 0f));
        if (_targets.Count == 0) return 0;
        int fired = 0;
        for (int t = 0; t < _targets.Count; t++) _aimed.Add(0);
        if (countInFlight != null)
        {
            foreach (var projectile in countInFlight.Active)
            {
                if (projectile.Shooter != _troop) continue;
                for (int t = 0; t < _targets.Count; t++)
                    if (_targets[t].soldier == projectile.Target) { _aimed[t]++; break; }
            }
        }
        Vector2 facing = _troop.Facing;

        for (int i = 0; i < _shooters.Count; i++)
        {
            if (i < _ready.Count && !_ready[i]) continue;
            SoldierAgentState shooter = _shooters[i];
            RangedWeaponProfile weapon = shooter.Character.RangedWeapon;
            Vector2 from = _shooterPositions[i];
            _options.Clear();
            for (int t = 0; t < _targets.Count; t++)
            {
                Vector2 position = targetTroop.WorldPosition(_targets[t].soldier);
                Vector2 offset = position - from;
                bool reachable = useFrontArc
                    ? RangedRules.CanReach(offset, facing, tuning.RangedFrontArcDeg, weapon.Range)
                    : offset.sqrMagnitude <= weapon.Range * weapon.Range;
                if (reachable) _options.Add((t, offset.sqrMagnitude, position));
            }
            if (_options.Count == 0) continue;
            if (_options.Count > 1) _options.Sort(_byPreference);
            int chosen = -1;
            foreach (var option in _options)
            {
                if (!CanSee(i, targetTroop, option.position)) continue;
                chosen = option.index;
                break;
            }
            if (chosen < 0) continue;
            _aimed[chosen]++;
            SoldierAgentState target = _targets[chosen].soldier;
            float hitChance = RangedRules.HitChance(weapon.Accuracy, shooter.Character.SkillFor(weapon.Class), tuning.BaseShotDC);
            projectiles.Launch(_troop, from, targetTroop, target, RangedRules.RawDamage(weapon.Damage), hitChance,
                tuning.ProjectileSpeed, weapon, shooter);
            onFired?.Invoke(shooter);
            fired++;
        }
        return fired;
    }
}
