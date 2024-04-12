using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Master : MonoBehaviour {
    public static Master Instance;
    public Dictionary<UnitR, Army> unitArmy;
    [SerializeField] Army player, enemy1;
    Dictionary<UnitR, List<UnitR>> combatFinder;
    HashSet<HashSet<UnitR>> combatList;
    void Awake () {
        if(Instance != null)
        {
            return;
        }
        else
        {
            Instance = this;
        }
        unitArmy = new Dictionary<UnitR, Army>();
        combatFinder = new();
        combatList = new();
    }
    public void CreateCombat(UnitR attacker, UnitR defender)
    {
        HashSet<UnitR> fight = new();
        fight.Add(attacker);
        fight.Add(defender);
        if (combatList.Contains(fight))
            return;
        combatList.Add(fight);
        attacker.state = UnitState.Fighting;
        defender.state = UnitState.Fighting;
    }
    public void EndCombat(UnitR attacker, UnitR defender)
    {
        HashSet<UnitR> fight = new();
        fight.Add(attacker);
        fight.Add(defender);
        if (!combatList.Contains(fight))
            return;
        attacker.state = UnitState.Idle;
        defender.state = UnitState.Idle;
        combatList.Remove(fight);
    }
	public void AddCombat(UnitR attacker, UnitR defender)
    {
        AddUnitCombat(attacker, defender);
        AddUnitCombat(defender, attacker);

    }
    void AddUnitCombat(UnitR unit, UnitR unit2)
    {
        if(!combatFinder.ContainsKey(unit)) {
            combatFinder.Add(unit, new());
        }
        unit.state = UnitState.Fighting;
        combatFinder[unit].Add(unit2);
    }
    public void RemoveCombat(UnitR unit, bool dead = false)
    {
        for (int i = combatFinder[unit].Count - 1; i >= 0; i--)
        {
            var enemy = combatFinder[unit][i];
            combatFinder[enemy].Remove(unit);
            if (combatFinder[enemy].Count == 0)
            {
                enemy.state = UnitState.Idle;
                combatFinder.Remove(enemy);
            }
        }
    }
    public void MakeAttack(UnitR unit)
    {
        if(!combatFinder.ContainsKey(unit)) { return; }
        float attacks = unit.models.Count / combatFinder[unit].Count;
        for (int i = combatFinder[unit].Count - 1; i >= 0; i--)
        {
            //Debug.Log(unit + " attacking " + combatFinder[unit][i]);    
            var enemy = combatFinder[unit][i];
            enemy.Die(unit.weapon.Attack(enemy));
        }
    }
}
