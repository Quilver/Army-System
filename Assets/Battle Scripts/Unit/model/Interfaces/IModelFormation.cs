using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ModelComponents
{
    public interface IModelFormation
    {
        public void SetUp(Rigidbody2D[] pins);
        public void SetPosition(Vector3 position, bool warpToPoint = false);
    }
}