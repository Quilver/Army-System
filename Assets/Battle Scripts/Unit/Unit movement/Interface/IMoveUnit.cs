using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SteeringSystem
{
    public interface IMoveUnit
    {
        void MoveUnit(Vector2 direction);
    }
}