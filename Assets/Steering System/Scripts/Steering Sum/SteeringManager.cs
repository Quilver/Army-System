using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SteeringManager : MonoBehaviour
{
    public abstract float RayLength { get; }
    public abstract Vector2 futurePosition { get; }
    public abstract RaycastHit2D[] SensorViews { get; }
    public abstract void AddSteeringForce(Vector2 direction, float priority);
    public abstract float MaxSpeed { get; }
    public Vector2 Seek(Vector2 target)
    {
        Vector2 desiredVelocity = (target - futurePosition).normalized * MaxSpeed;
        Vector2 steerVelocity = desiredVelocity - GetComponent<Rigidbody2D>().velocity;
        return steerVelocity;
    }
    public Vector2 GetDirection(int i)
    {
        float angle = i * 360f / SensorViews.Length + transform.rotation.z * Mathf.Deg2Rad;
        return Quaternion.AngleAxis(angle, transform.forward) * transform.up;
    }
    public Vector2 GetNormalOfDirection(int i, bool left)
    {
        float angle = i * 360f / SensorViews.Length + transform.rotation.z * Mathf.Deg2Rad;
        if (left) angle += 90;
        else angle -= 90;
        return Quaternion.AngleAxis(angle, transform.forward) * transform.up;
    }
    public LayerMask SensorLayerMask;
    public bool CanWalkTo(Vector2 destination)
    {
        Vector2 direction = destination - (Vector2)transform.position;
        float angle = Vector2.SignedAngle(Vector2.up, direction);
        float distance = direction.magnitude;
        var hit = Physics2D.BoxCast(transform.position, transform.localScale * 0.5f, 0, direction, distance, SensorLayerMask);
        return !hit;
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log(gameObject.name + " has gotten hit");
    }
}
