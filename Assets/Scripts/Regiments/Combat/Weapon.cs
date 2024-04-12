using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
[System.Serializable]
public class Weapon
{
    readonly UnitR unit;
    public Weapon(UnitR unit) { this.unit = unit; }
    int Attacks
    {
        get
        {
            if (unit.models.Count < 6) { return 4; }
            else if (unit.models.Count < 10) { return 6; }
            else if (unit.models.Count < 16) { return 8; }
            else return 10;
        }
    }
    bool Flanking (UnitR target)
    {
        return !target.Movement.InCombatWith(target.Movement.position, unit);
    }
    int RelativeSkill(UnitR target)
    {
        int relative = unit.stats.Power - target.stats.Defence;
        return relative;
    }
    int RollDamage(int attacks, int skillDifference)
    {
        int roll1 = UnityEngine.Random.Range(0, attacks);
        int roll2 = UnityEngine.Random.Range(0, attacks);
        if (skillDifference > 0) return math.max(roll1, roll2);
        else if (skillDifference < 0) return math.min(roll1, roll2);
        else return (roll1 + roll2) / 2;
    }
    public int Attack (UnitR target)
    {
        var damage = RollDamage(Attacks, RelativeSkill(target));
        if (Flanking(target)) damage += RollDamage(Attacks, RelativeSkill(target));
        return damage;
    }
    float _time;
    int _counter;
    public void StartCombat()
    {
        _time = 0;
        _counter = 0;
        
    }
    public void UpdateCombat()
    {
        _time += Time.deltaTime;
        
    }
    void DetermineAttack()
    {

    }
}