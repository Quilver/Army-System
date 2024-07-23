using System;
using System.Collections;
using System.Collections.Generic;
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
        #endregion
        #region Methods
        public void MoveTo(Vector2 location);
        public void MoveTo(UnitBase unit);
        #endregion
    }
}