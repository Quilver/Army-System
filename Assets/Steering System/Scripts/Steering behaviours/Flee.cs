using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flee : SteeringBehaviour
{
    [SerializeField]
    float maxDistance = 2;

    public override float priority => 0.9f;

    public override Vector2 GetDirection()
    {
        foreach (var collision in Physics2D.OverlapCircleAll(parent.transform.position, maxDistance))
        {
            if (collision.gameObject == parent.gameObject) continue;
            if(Vector3.Distance(collision.transform.position, parent.transform.position) > maxDistance) continue;
            if(collision.transform.gameObject.GetComponent<ContextSteerer>() == null) continue;
            var point = collision.ClosestPoint(parent.transform.position) + collision.transform.gameObject.GetComponent<Rigidbody2D>().velocity;
            parent.AddSteeringForce(-parent.Seek(point), 1);
        }
        
        return Vector2.zero;
    }
}
