using System.Collections.Generic;
using UnityEngine;

// Projectiles in flight (spec §4.7). Plain state, pooled, drawn by ProjectileView. Shots are not instant: the hit is
// rolled when the projectile lands (Q-42). Only the target enemy soldier can be hit: no friendly fire and no blocking
// (Q-40). Damage goes through the target's armour (no Strength, no flank bonus, Q-5).
public sealed class TroopProjectiles
{
    public sealed class Projectile
    {
        public Troop Shooter;
        // The soldier that shot it, for the combat log. May be null.
        public SoldierAgentState ShooterSoldier;
        public Troop TargetTroop;
        public SoldierAgentState Target;
        public Vector2 From;
        public Vector2 To;
        public float FlightTime;
        public float Age;
        public int Damage;
        // The weapon that fired it; its Piercing / Bludgeoning apply to the armour roll (SU-89). May be null.
        public WeaponProfile Weapon;
        public float HitChance;

        public Vector2 Position => Vector2.Lerp(From, To, FlightTime > 0f ? Mathf.Clamp01(Age / FlightTime) : 1f);
    }

    readonly List<Projectile> _active = new();
    readonly Stack<Projectile> _pool = new();

    public IReadOnlyList<Projectile> Active => _active;

    public void Launch(Troop shooter, Vector2 from, Troop targetTroop, SoldierAgentState target, int damage,
        float hitChance, float speed, WeaponProfile weapon = null, SoldierAgentState shooterSoldier = null)
    {
        Projectile projectile = _pool.Count > 0 ? _pool.Pop() : new Projectile();
        projectile.Shooter = shooter;
        projectile.ShooterSoldier = shooterSoldier;
        projectile.TargetTroop = targetTroop;
        projectile.Target = target;
        projectile.From = from;
        projectile.To = targetTroop.WorldPosition(target);
        projectile.FlightTime = Vector2.Distance(from, projectile.To) / Mathf.Max(0.01f, speed);
        projectile.Age = 0f;
        projectile.Damage = damage;
        projectile.Weapon = weapon;
        projectile.HitChance = hitChance;
        _active.Add(projectile);
    }

    public void Tick(float deltaTime)
    {
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            Projectile projectile = _active[i];
            projectile.Age += deltaTime;
            if (projectile.Age < projectile.FlightTime) continue;
            Land(projectile);
            _active.RemoveAt(i);
            projectile.Shooter = null;
            projectile.ShooterSoldier = null;
            projectile.TargetTroop = null;
            projectile.Target = null;
            projectile.Weapon = null;
            _pool.Push(projectile);
        }
    }

    public void Clear()
    {
        foreach (var projectile in _active) _pool.Push(projectile);
        _active.Clear();
    }

    static void Land(Projectile projectile)
    {
        Troop shooter = projectile.Shooter;
        Troop targetTroop = projectile.TargetTroop;
        if (targetTroop == null || projectile.Target == null || !projectile.Target.IsAlive) return;
        ITroopRandom random = shooter != null ? shooter.TroopRandom : targetTroop.TroopRandom;
        float hitRoll = random.Value();
        bool hit = hitRoll < projectile.HitChance;
        if (shooter != null) shooter.MeleeMetrics.RecordShot(hit);
        BattleEvents.RaiseProjectileLanded(shooter, targetTroop, hit, targetTroop.WorldPosition(projectile.Target));
        var entry = new CombatLogEntry
        {
            Kind = CombatLogKind.Shot,
            Time = CombatLog.Now,
            Attacker = CombatLogSoldier.Of(shooter, projectile.ShooterSoldier),
            Defender = CombatLogSoldier.Of(targetTroop, projectile.Target),
            Weapon = projectile.Weapon != null ? projectile.Weapon.DisplayName : null,
            HitChance = projectile.HitChance,
            HitRoll = hitRoll,
            Hit = hit,
            Damage = new CombatLogDamage { WeaponDamage = projectile.Damage, Raw = projectile.Damage },
        };
        if (!hit)
        {
            CombatLog.Record(entry);
            return;
        }
        Vector2 origin = shooter != null ? (Vector2)shooter.transform.position : projectile.From;
        AppliedDamage taken = targetTroop.ApplyDamageDetailed(projectile.Target, projectile.Damage, origin, flanked: false,
            shooter, projectile.Weapon, out MitigationResult mitigation);
        int applied = taken.Total;
        if (shooter != null) shooter.MeleeMetrics.RecordDamageDone(applied);
        targetTroop.MeleeMetrics.RecordDamageReceived(applied);
        DuelResolver.FillDamage(ref entry.Damage, mitigation, applied, projectile.Target);
        CombatLog.Record(entry);
    }
}
