using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ModelComponents
{
    public interface IModelFormation
    {
        public void SetUp(Rigidbody2D[] pins, Vector2 offsetPos, Transform unit);
        public void SetPosition(Vector3 position, Vector2 offsetPos, bool warpToPoint = false);
    }
}