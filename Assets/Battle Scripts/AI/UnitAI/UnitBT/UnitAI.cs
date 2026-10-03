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
        public virtual void GiveOrder(UnitBehaviour order, IQuery exitCondition) { }
        
        protected virtual float TimeUntilNextDecision => 1;
        protected abstract void TakeDecision();
        [SerializeField, Header("Last action taken")]
        protected UnitBehaviour LastBehaviour;
        public string _lastBehaviour;
        public virtual void DebugGizmos()
        {
            if(LastBehaviour != null)
                LastBehaviour.DrawDebug();
        }
        protected void ExecuteOrder(UnitBehaviour behaviour)
        {
            behaviour.MakeMove();
            LastBehaviour = behaviour;
            _lastBehaviour = behaviour.GetType().ToString();
        }
    }
    
}
