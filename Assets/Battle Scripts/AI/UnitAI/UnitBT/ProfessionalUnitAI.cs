using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AISystem.Behaviour;
namespace AISystem.UnitAI
{

    [System.Serializable]
    public class ProfessionalUnitAI : UnitAI
    {
        [SerializeReference, SubclassSelector]
        UnitBehaviour Idle, FormUp, Attack;

        protected override void SetupUnitBehaviours()
        {
            Idle.Setup(unit, squad, influenceMap);
            FormUp.Setup(unit, squad, influenceMap);
            Attack.Setup(unit, squad, influenceMap);
        }

        protected override void TakeDecision()
        {
            if (influenceMap.relevantEnemies.Count == 0)
                ExecuteOrder(Idle);
            else if (!InPosition())
                ExecuteOrder(FormUp);
            else
                ExecuteOrder(Attack);
        }
        bool InPosition()
        {
            return false;
        }
    }
    
}
