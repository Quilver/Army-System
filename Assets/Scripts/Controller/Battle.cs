using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Battle : MonoBehaviour {
    public static Battle Instance;
    public Dictionary<UnitInterface, Army> unitArmy;
    [SerializeField] Army player, enemy1;
    Dictionary<UnitInterface, HashSet<UnitInterface>> combatList;
    void Awake () {
        if(Instance != null)
        {
            return;
        }
        else
        {
            Instance = this;
        }
        unitArmy = new Dictionary<UnitInterface, Army>();
        combatList = new();
    }
    public bool Enemies(UnitR unit1, UnitR unit2)
    {
        return unitArmy[unit1] != unitArmy[unit2];
    }
    public void CreateCombat(UnitR attacker, UnitR defender)
    {
        if (!Enemies(attacker, defender)) return;
        if (!combatList.ContainsKey(attacker))
        {
            combatList.Add(attacker, new());
            attacker.Melee.StartCombat();
        }
        else if (combatList[attacker].Contains(defender))
            return;
        if (!combatList.ContainsKey(defender))
        {
            combatList.Add(defender, new());
            defender.Melee.StartCombat();
        }
        else if (combatList[defender].Contains(attacker))
            return;
        combatList[attacker].Add(defender);
        combatList[defender].Add(attacker);
        attacker.State = UnitState.Fighting;
        defender.State = UnitState.Fighting;
    }
    public void EndCombat(UnitR attacker, UnitR defender)
    {
        if (!combatList.ContainsKey(attacker) || !combatList.ContainsKey(defender))
            return;
        if(combatList.ContainsKey(attacker) && combatList[attacker].Contains(defender))
        {
            combatList[attacker].Remove(defender);
            if (combatList[attacker].Count == 0)
            {
                combatList.Remove(attacker);
                attacker.State = UnitState.Idle;
            }
        }
        if (combatList.ContainsKey(defender) && combatList[defender].Contains(attacker))
        {
            combatList[defender].Remove(attacker);
            if (combatList[defender].Count == 0)
            {
                combatList.Remove(defender);
                defender.State = UnitState.Idle;
            }
        }
        
    }
    HashSet<UnitInterface> slainUnits=new();
    public void EndCombat(UnitInterface fighter)
    {
        slainUnits.Add(fighter);
    }
    void UpdateForDeadUnits(UnitInterface fighter)
    {
        if (!combatList.ContainsKey(fighter))
            return;
        var enemies = combatList[fighter];
        foreach (var enemy in enemies)
        {
            combatList[enemy].Remove(fighter);
            if (combatList[enemy].Count == 0)
            {
                combatList.Remove(enemy);
                enemy.State = UnitState.Idle;
            }
        }
        combatList.Remove(fighter);
        //Destroy(fighter.gameObject);
    }
    private void Update()
    {
        UpdateCombats();
        UpdateVictory();
    }
    void UpdateCombats()
    {
        foreach (var attacker in combatList)
        {
            if(attacker.Value.Count == 0)
            {
                combatList.Remove(attacker.Key);
                continue;
            }
            attacker.Key.Melee.UpdateCombat(attacker.Value);
        }
        foreach (var unit in combatList)
        {
            if (unit.Key.ModelsRemaining == 0)
                slainUnits.Add(unit.Key);
        }
        foreach (var deadUnit in slainUnits)
        {
            UpdateForDeadUnits(deadUnit);
        }
        slainUnits.Clear();
    }
    void UpdateVictory()
    {

    }
}
