using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
[System.Serializable]
public class Weapon
{
    UnitR unit;
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
        foreach (var model in target.models)
        {
            UnitR unitAhead1 = Map.Instance.getTile(model.ModelPosition + target.Movement.position.direction).unit;
            UnitR unitAhead2 = Map.Instance.getTile(model.ModelPosition + target.Movement.position.direction * 2).unit;
            if (unitAhead1 == unit || unitAhead2 == unit) return false;
        }
        return true;
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
}
