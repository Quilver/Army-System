using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace AISystem
{
    [System.Serializable]
    public abstract class UnitBehaviour 
    {
        [SerializeField, HideInInspector]
        public IUnit unit;
        [SerializeField, HideInInspector]
        public ISquad squad;
        [SerializeField, HideInInspector]
        public InfluenceMap map;
        public virtual void Setup(IUnit unit, ISquad squad, InfluenceMap map)
        {
            this.unit = unit;
            this.squad = squad;
            this.map = map;
        }
        public virtual void MakeMove()
        {
            if (unit == null) MakeSquadMove();
            else MakeUnitMove();
        }
        protected abstract void MakeUnitMove();
        protected virtual void MakeSquadMove() =>throw new System.NotImplementedException();
        [SerializeField] bool showDebug;
        public virtual void DrawDebug()
        {
            if (!showDebug)return;
        }
    }
    
    
    
}

