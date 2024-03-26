using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Master : MonoBehaviour {
    public static Master Instance;
    public Dictionary<UnitR, Army> unitArmy;
    [SerializeField] Army player, enemy1;
    List<Combats> combats;
    Dictionary<UnitR, List<UnitR>> combatFinder;
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
        combats= new ();
        combatFinder = new();
    }
	public void AddCombat(UnitR attacker, UnitR defender)
    {
        Combats combat = new Combats (attacker, defender);
        combats.Add(combat);
        AddCombat(attacker, defender, combat);
        AddCombat(defender, attacker, combat);

    }
    void AddCombat(UnitR unit, UnitR unit2, Combats combat)
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
            enemy.Die(4);
        }
    }
}
