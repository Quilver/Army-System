using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AchievementSystem.EndBattle
{
    public class Rout : MonoBehaviour
    {
        [SerializeField]
        bool WinCondition;
        [SerializeField]
        Army _army;
        void Start()
        {
            if(_army == null) Debug.LogError($"{gameObject.name} has not been assigned army for rout exit");
            IUnit.OnUnitDestroyed += CheckUnitsRemaining;
        }
        void CheckUnitsRemaining(IUnit unitDestroyed)
        {
            if(_army == null)return;
            if(_army.Units.Count > 1) return;
            IUnit.OnUnitDestroyed -= CheckUnitsRemaining;
            Battle.Instance.EndBattle(WinCondition);
        }
    }
}