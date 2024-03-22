using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class Combats
{
    [SerializeField]
    Dictionary<Unit, List<Unit>> combats = new Dictionary<Unit, List<Unit>>();
    public void AddCombat(Unit unit1, Unit unit2)
    {
        if (!combats.ContainsKey(unit1)) combats.Add(unit1, new List<Unit>());
        combats[unit1].Add(unit2);
        if (!combats.ContainsKey(unit2)) combats.Add(unit2, new List<Unit>());
        combats[unit2].Add(unit1);
    }
    public void BreakCombat(Unit unit)
    {
        if (!combats.ContainsKey(unit)) return;
        var fighting = combats[unit];
        foreach (var enemy in fighting)
        {
            combats[enemy].Remove(unit);
            if (combats[enemy].Count == 0) {
                enemy.EndCombat();
                combats.Remove(enemy);
            } 
        }
        combats.Remove(unit);
    }
    public void MakeAttacks(Unit attacker)
    {
        if (!combats.ContainsKey(attacker))
        {
            Debug.LogError("does not have any combatants");
            return;
        }
        int attacks = attacker.models.Count;
        MakeAttacks(attacker, attacks, combats[attacker][0]);
    }
    public bool InCombat(Unit unit) { 
    return combats.ContainsKey(unit);
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
        int probability = 35 + 5 * (stat1 - stat2);
        Math.Clamp(probability, 15, 80);
        for (int i = 0; i < numberOfTests; i++)
        {
            if (probability >= UnityEngine.Random.Range(0, 100)) { successes++; }
        }
        return successes;
    }
}
