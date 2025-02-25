using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Separate : SteeringBehaviour
{
    [SerializeField, Range(0.5f, 3)]
    float maxDistance = 1f;
    [SerializeField, Range(0.2f, 1.5f)]
    float HardMin = 0.5f;
    [SerializeField, Range(0.1f, 1)]
    float _priority;
    public override float priority => _priority;
    float WeightedPriority(float distance)
    {
        return priority * Mathf.InverseLerp(maxDistance, HardMin, distance); 
    }
    public override Vector2 GetDirection()
    {
        Vector2 sumForce = Vector2.zero;
        foreach (var sensor in parent.SensorViews)
        {
            if(!sensor)continue;
            if(sensor.collider.transform == target) continue;
            float distance = Vector2.Distance(parent.transform.position, sensor.point);
            if (sensor.collider == null) distance = 100;
            if (distance > maxDistance)continue;
            parent.AddSteeringForce(sensor.point - (Vector2)parent.transform.position, -WeightedPriority(distance));
            parent.AddSteeringForce((Vector2)parent.transform.position - sensor.point, WeightedPriority(distance));
        }
        
        return sumForce;
    }
    public bool DrawGizmo=true;
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        if (parent == null || !enabled || !DrawGizmo) return;
        Gizmos.DrawWireSphere(parent.transform.position, maxDistance);
        Gizmos.color= Color.red;
        Gizmos.DrawWireSphere(parent.transform.position, HardMin);
        foreach (var collision in Physics2D.OverlapCircleAll(parent.transform.position, maxDistance))
        {
            if (collision.transform == parent) continue;
            var point = collision.ClosestPoint(parent.transform.position);
            Gizmos.DrawLine(parent.transform.position, point);
        }
    }
}