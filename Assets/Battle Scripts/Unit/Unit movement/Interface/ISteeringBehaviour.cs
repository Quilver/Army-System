using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SteeringSystem
{
    public interface ISteeringBehaviour
    {
        Vector2 GetForce();
        void AddForce();

    }

}
