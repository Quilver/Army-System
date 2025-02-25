using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class Advance : SteeringBehaviour
{
    [SerializeField, Range(0.1f, 1)]
    float _priority;
    public override float priority => _priority;
    public override Vector2 GetDirection() {
        parent.AddSteeringForce(parent.transform.up, priority);
        return parent.transform.up;
    }
}
