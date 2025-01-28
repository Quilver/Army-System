using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeekTarget : SteeringBehaviour
{
    [SerializeField]
    Transform target;
    [SerializeField, Range(0.1f, 1)]
    float _priority;
    public override float priority => _priority;

    public override Vector2 GetDirection()
    {
        parent.CanWalkTo(target.position);
        parent.AddSteeringForce(parent.Seek(target.position), priority);
        return parent.Seek(target.position);
    }
}
