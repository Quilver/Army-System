using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Hold : SteeringBehaviour
{
    [SerializeField, Range(0.1f, 1)]
    float _priority = 1;
    public override float priority => _priority;

    public override Vector2 GetDirection()
    {

        //parent.AddSteeringForce(sensor.point - (Vector2)parent.transform.position, priority);
        return Vector2.zero;
    }
}
