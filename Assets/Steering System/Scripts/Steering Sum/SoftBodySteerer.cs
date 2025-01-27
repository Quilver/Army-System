using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SoftBodySteerer : MonoBehaviour
{
    [SerializeField, Range(0.1f, 3)]
    float maxForce;
    [SerializeField]
    Transform target;
    List<Rigidbody2D> rigidBodies;
    Vector2 Velocity
    {
        get
        {
            if (rigidBodies == null) return Vector2.zero;
            Vector2 velocity = Vector2.zero;
            foreach (var rigidBody in rigidBodies)
                { velocity += rigidBody.velocity; }
            return velocity;
        }
    }
    float Mass
    {
        get
        {
            if (rigidBodies == null) return 1;
            float mass = 0;
            foreach (var rigidBody in rigidBodies)
            { mass += rigidBody.mass; }
            return mass;
        }
    }
    void Start()
    {
        rigidBodies= GetComponentsInChildren<Rigidbody2D>().ToList();
    }
    void Update()
    {
        AddForce(Vector2.ClampMagnitude(Seek(target.position), maxForce));
        Vector2 lookdirection = Velocity;
        float angle = Mathf.Atan2(lookdirection.y, lookdirection.x) * Mathf.Rad2Deg - 90.0f;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }
    void AddForce(Vector2 direction)
    {
        foreach (var rigidBody in rigidBodies)
            rigidBody.AddForce(direction);
    }
    Vector2 Seek(Vector2 target)
    {
        Vector2 desiredVelocity = ((Vector3)target - transform.position).normalized * 10 * maxForce;
        Vector2 steerVelocity = desiredVelocity - Velocity;
        return steerVelocity;
    }

}
