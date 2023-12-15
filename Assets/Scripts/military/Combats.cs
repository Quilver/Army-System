using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Combats
{
    [SerializeField]
    List<Unit> combats;
    public Combats(Unit unit1, Unit unit2)
    {
        AddUnit(unit1);
        AddUnit(unit2);
    }
    void AddUnit(Unit unit)
    {
        if (combats.Contains(unit)) return;
        combats.Add(unit);
    }
    public void MakeAttacks(Unit attacker, int attacks, Unit defender)
    {
        int hits = Combats.AbilityCheck(attacker.Stats.WeaponSkill, defender.Stats.WeaponSkill, attacks);
        int wounds = Combats.AbilityCheck(attacker.Stats.AttackStrength, defender.Stats.Defence, hits);
        defender.deaths(wounds);
    }
    public static int AbilityCheck(int stat1, int stat2, int numberOfTests)
    {
        int successes = 0;
        int probability = 15 + (stat1 - stat2);
        Math.Clamp(probability, 5, 80);
        for (int i = 0; i < numberOfTests; i++)
        {
            if (probability >= UnityEngine.Random.Range(0, 100)) { successes++; }
        }
        return successes;
    }
}
