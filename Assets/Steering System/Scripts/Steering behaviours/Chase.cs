using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chase : SteeringBehaviour
{
    [SerializeField]
    Rigidbody2D target;

    public override float priority => 0.8f;

    public override Vector2 GetDirection()
    {
        Vector2 targetPosition = target.position + target.velocity;
        parent.AddSteeringForce(parent.Seek(targetPosition), 1);
        return Vector2.zero;
    }
}