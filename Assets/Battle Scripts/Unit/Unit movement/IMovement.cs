using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
namespace UnitMovement
{
    public interface IMovement
    {
        #region Properties
        public Vector2 Location { get; }
        public float Rotation { get; }
        public int Files { get; }
        public int Ranks { get; }
        protected RegimentSizer unitBody { get;}
        protected ChargeSizer charge { get;}
        #endregion
        #region Queries
        public bool InCombatWith(Vector2 position, float angle, UnitBase target)
        {
            List<UnitBase> targets = charge.TargetsAt(position, angle);
            return targets.Contains(target);
        }
        #endregion
        #region Methods
        public void MoveTo(Vector2 location);
        public void MoveTo(UnitBase unit);

        #endregion
    }
}