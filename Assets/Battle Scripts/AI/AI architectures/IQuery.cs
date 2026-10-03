using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AISystem
{
    [System.Serializable]
    public abstract class IQuery
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
        public abstract bool Query();
    }
}