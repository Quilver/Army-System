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
        public Troop TargetTroop;
        public SoldierAgentState Target;
        public Vector2 From;
        public Vector2 To;
        public float FlightTime;
        public float Age;
        public int Damage;
        public float HitChance;

        public Vector2 Position => Vector2.Lerp(From, To, FlightTime > 0f ? Mathf.Clamp01(Age / FlightTime) : 1f);
    }

    readonly List<Projectile> _active = new();
    readonly Stack<Projectile> _pool = new();

    public IReadOnlyList<Projectile> Active => _active;

    public void Launch(Troop shooter, Vector2 from, Troop targetTroop, SoldierAgentState target, int damage,
        float hitChance, float speed)
    {
        Projectile projectile = _pool.Count > 0 ? _pool.Pop() : new Projectile();
        projectile.Shooter = shooter;
        projectile.TargetTroop = targetTroop;
        projectile.Target = target;
        projectile.From = from;
        projectile.To = targetTroop.WorldPosition(target);
        projectile.FlightTime = Vector2.Distance(from, projectile.To) / Mathf.Max(0.01f, speed);
        projectile.Age = 0f;
        projectile.Damage = damage;
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
            projectile.TargetTroop = null;
            projectile.Target = null;
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
        bool hit = random.Value() < projectile.HitChance;
        if (shooter != null) shooter.MeleeMetrics.RecordShot(hit);
        if (!hit) return;
        Vector2 origin = shooter != null ? (Vector2)shooter.transform.position : projectile.From;
        int applied = targetTroop.ApplyDamage(projectile.Target, projectile.Damage, origin, flanked: false);
        if (shooter != null) shooter.MeleeMetrics.RecordDamageDone(applied);
        targetTroop.MeleeMetrics.RecordDamageReceived(applied);
    }
}
