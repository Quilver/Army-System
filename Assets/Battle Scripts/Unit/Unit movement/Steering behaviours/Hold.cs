using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Hold : SteeringBehaviour
{
    [SerializeField, Range(0.1f, 1)]
    float _priority = 1;
    public override float priority => _priority;

    public override void GetDirection()
    {

    }
}
