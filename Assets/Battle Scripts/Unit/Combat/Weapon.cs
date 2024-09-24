using MyNamespace;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnitMovement;
using UnityEngine;
[System.Serializable]
public class Weapon
{
    readonly UnitBase unit;
    readonly StatSystem.RegimentStats regimentStats;
    public Weapon(UnitBase unit) {
        _enemies = new();
        Notifications.StartFight += StartFight;
        Notifications.EndFight += EndFight;
        Notifications.Died += Death;
        this.unit = unit;
        regimentStats = unit.UnitStats as StatSystem.RegimentStats;
    }
    HashSet<UnitBase> _enemies;
    void StartFight(UnitBase unit1, UnitBase unit2)
    {
        if (unit1 != unit && unit2 != unit) return;
        if(!Battle.Instance.Enemies(unit1, unit2)) return;
        if(_enemies.Count == 0)
            StartCombat();
        if(unit1 != unit) _enemies.Add(unit1);
        else if(unit2 != unit) _enemies.Add(unit2);
    }
    void EndFight(UnitBase unit1, UnitBase unit2)
    {
        if (unit1 != unit && unit2 != unit) return;
        if (unit1 != unit) _enemies.Remove(unit1);
        else if (unit2 != unit) _enemies.Remove(unit2);
        if (_enemies.Count == 0) unit.State = UnitState.Idle;
    }
    void Death(UnitBase unit)
    {
        if (this.unit != unit) return;
        _enemies.Clear();
        Notifications.StartFight-= StartFight;
        Notifications.EndFight-= EndFight;
        Notifications.Died -= Death;
    }
    bool Flanking (UnitBase target)
    {
        return !((IMovement)target.Movement).InCombatWith(target.Movement.Location, target.Movement.Rotation, unit);
    }
    float WoundedModifier {
        get {
            if (unit.Wounded)
                return 0.5f;
            else
                return 1;
        }
    }
    
    float _time, _damageDone, _damageModifier;
    UnitBase _target;
    readonly float TIMECYCLE = 6;
    public void StartCombat()
    {
        _damageDone = 0;
        _target = null;
        unit.State= UnitState.Fighting;
        DetermineAttack();
    }
    public void UpdateCombat()
    {
        _time += Time.deltaTime;
        if (_time > TIMECYCLE) DetermineAttack();
        if (_target == null)
            DetermineTarget(_enemies);
        _damageDone += Time.deltaTime * WoundedModifier;
        float flankBonus = 1;
        if (Flanking(_target))
            flankBonus = 3;
        if (_damageDone * _damageModifier * flankBonus >= ((StatSystem.IDefenceStats)_target.UnitStats).Defence)
        {
            Notifications.MeleeDamage(unit, _target, 1);
            _target.TakeDamage(1);
            DetermineTarget(_enemies);
            _damageDone= 0;
        }
    }
    void DetermineTarget(HashSet<UnitBase> enemy)
    {
        if(enemy.Count == 0) Debug.LogError(unit.ToString() + " has no enemies");
        var enemies = enemy.ToList();
        _target = enemies[UnityEngine.Random.Range(0, enemies.Count)];
    }

    void DetermineAttack()
    {
        _time = 0;
        float min = 1;
        float max = 1.5f;
        _damageModifier = UnityEngine.Random.Range(min, max) * ((ICombatStats)regimentStats).AttackSpeed;
    }
}
