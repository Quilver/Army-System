using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AISystem.UnitAI
{
    public abstract class UnitAI
    {

        
        protected IUnit unit;
        protected ISquad squad;
        protected InfluenceMap influenceMap;
        public virtual void Setup(IUnit unit, ISquad squad, InfluenceMap influenceMap)
        {
            this.unit = unit;
            this.squad = squad;
            this.influenceMap = influenceMap;
            SetupUnitBehaviours();
        }
            
        protected abstract void SetupUnitBehaviours();
        public virtual IEnumerator RunAI()
        {
            
            yield return null;
            while(unit != null)
            {
                TakeDecision();
                yield return new WaitForSeconds(TimeUntilNextDecision);
            }
        }
        protected virtual float TimeUntilNextDecision => 3;
        protected abstract void TakeDecision();
        [SerializeField]
        protected string LastBehaviour;
        protected void ExecuteOrder(UnitBehaviour behaviour)
        {
            behaviour.MakeMove();
            LastBehaviour = behaviour.GetType().ToString();
        }
    }
    
}
