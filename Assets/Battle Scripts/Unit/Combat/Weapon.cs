using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
[System.Serializable]
public class Weapon
{
    readonly UnitInterface unit;
    readonly StatSystem.RegimentStats regimentStats;
    public Weapon(UnitInterface unit) {
        _enemies = new();
        Notifications.StartFight += StartFight;
        Notifications.EndFight += EndFight;
        Notifications.Died += Death;
        this.unit = unit;
        regimentStats = unit.UnitStats as StatSystem.RegimentStats;
    }
    HashSet<UnitInterface> _enemies;
    void StartFight(UnitInterface unit1, UnitInterface unit2)
    {
        if (unit1 != unit && unit2 != unit) return;
        if(!Battle.Instance.Enemies(unit1 as UnitR, unit2 as UnitR)) return;
        if(_enemies.Count == 0)
            StartCombat();
        if(unit1 != unit) _enemies.Add(unit1);
        else if(unit2 != unit) _enemies.Add(unit2);
    }
    void EndFight(UnitInterface unit1, UnitInterface unit2)
    {
        if (unit1 != unit && unit2 != unit) return;
        if (unit1 != unit) _enemies.Remove(unit1);
        else if (unit2 != unit) _enemies.Remove(unit2);
        if (_enemies.Count == 0) unit.State = UnitState.Idle;
    }
    void Death(UnitInterface unit)
    {
        if (this.unit != unit) return;
        _enemies.Clear();
        Notifications.StartFight-= StartFight;
        Notifications.EndFight-= EndFight;
        Notifications.Died -= Death;
    }
    bool Flanking (UnitInterface target)
    {
        return !target.Movement.InCombatWith(target.Movement.position, unit);
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
    UnitInterface _target;
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
    void DetermineTarget(HashSet<UnitInterface> enemy)
    {
        if(enemy.Count == 0) Debug.LogError(unit.ToString() + " has no enemies");
        var enemies = enemy.ToList();
        _target = enemies[UnityEngine.Random.Range(0, enemies.Count)];
    }

    void DetermineAttack()
    {
        _time = 0;
        _damageModifier = UnityEngine.Random.Range(0.75f, 0.125f) * regimentStats.AttackPower.BaseStat;
    }
}
