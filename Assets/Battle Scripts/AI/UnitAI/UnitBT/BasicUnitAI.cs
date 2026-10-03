using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AISystem.Behaviour;
namespace AISystem.UnitAI
{

    [System.Serializable]
    public class BasicUnitAI : UnitAI
    {
        [Header("Queries")]
        [SerializeReference, SubclassSelector]
        public IQuery Enemies;
        [SerializeReference, SubclassSelector]
        public IQuery SquadMoving;//, CaptureTarget;
        [Header("Behaviours")]
        [SerializeReference, SubclassSelector]
        public UnitBehaviour Idle;
        [SerializeReference, SubclassSelector]
        public UnitBehaviour FollowUnit, Attack;//, CapturePoint;

        protected override void SetupUnitBehaviours()
        {
            //Setup Queries
            Enemies.Setup(unit, squad, influenceMap);
            SquadMoving.Setup(unit, squad, influenceMap);
            //Setup Behaviours
            Idle.Setup(unit, squad, influenceMap);
            Attack.Setup(unit, squad, influenceMap);
            FollowUnit.Setup(unit, squad, influenceMap);
        }

        protected override void TakeDecision()
        {
            if (Enemies.Query())
                ExecuteOrder(Attack);
            else if(SquadMoving.Query())
                ExecuteOrder(FollowUnit);
            else
                ExecuteOrder(Idle);
        }
    }
    

}
