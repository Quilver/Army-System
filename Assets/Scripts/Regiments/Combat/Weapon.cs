using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
[System.Serializable]
public class Weapon
{
    readonly UnitR unit;
    public Weapon(UnitR unit) { this.unit = unit; }
    bool Flanking (UnitR target)
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
    UnitR _target;
    float TIMECYCLE = 6;
    public void StartCombat()
    {
        _damageDone = 0;
        _target = null;
        DetermineAttack();
    }
    public void UpdateCombat(HashSet<UnitR> enemy)
    {
        _time += Time.deltaTime;
        if (_time > TIMECYCLE) DetermineAttack();
        if (_target == null)
            DetermineTarget(enemy);
        _damageDone += Time.deltaTime * WoundedModifier;
        float flankBonus = 1;
        if (Flanking(_target))
            flankBonus = 3;
        if (_damageDone * _damageModifier * flankBonus >= _target.stats.Defence)
        {
            _target.Die(1);
            DetermineTarget(enemy);
            _damageDone= 0;
        }
    }
    void DetermineTarget(HashSet<UnitR> enemy)
    {
        if(enemy.Count == 0) Debug.LogError(unit.ToString() + " has no enemies");
        var enemies = enemy.ToList();
        _target = enemies[UnityEngine.Random.Range(0, enemies.Count)];
    }
    void DetermineAttack()
    {
        _time = 0;
        _damageModifier = UnityEngine.Random.Range(0.8f, 1.2f);
    }
}
