using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleRound : SteeringBehaviour
{
    [SerializeField, Range(0.1f, 1)]
    float Priority;
    public override float priority => Priority;
    [SerializeField, Range(1f, 3)]
    float MaxRange;
    [SerializeField, Range(0.5f, 2)]
    float MinRange;
    public override Vector2 GetDirection()
    {
        var unitDirection = body.velocity.normalized;
        
        float unitDirectionAngle = Vector2.Angle(Vector2.zero, unitDirection);
        for (var i = 0; i < parent.SensorViews.Length; i++) {
            if(GetLateralForce(i) == Vector2.zero)continue;
            float weight = WeightedPriority(parent.RayLength, MinRange, parent.SensorViews[i].distance);
            var seek = parent.Seek(parent.futurePosition + GetLateralForce(i));
            parent.AddSteeringForce(seek, weight);
        }

        return Vector2.zero;
    }
    Vector2 GetLateralForce(int i)
    {
        if (!parent.SensorViews[i]) return Vector2.zero;
        var unitDirection = body.velocity.normalized;
        if (Vector2.Dot(unitDirection, parent.GetDirection(i)) < 0.5f) return Vector2.zero;
        float unitDirectionAngle = Vector2.Angle(Vector2.zero, unitDirection);
        var hitDirection = (parent.SensorViews[i].point - (Vector2)parent.transform.position).normalized;
        float hitAngle = Vector2.Angle(Vector2.zero, hitDirection);
        if (Mathf.DeltaAngle(unitDirectionAngle, hitAngle) > 0)
            return parent.GetNormalOfDirection(i, false);
        else
            return parent.GetNormalOfDirection(i, true);
    }
    public bool DrawGizmo = true;
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        if (parent == null || !enabled || !DrawGizmo) return;
        var unitDirection = body.velocity.normalized;
        float unitDirectionAngle = Vector2.Angle(Vector2.zero, unitDirection);
        for (var i = 0; i < parent.SensorViews.Length; i++)
        {
            
            var offset = Vector2.Dot(unitDirection, parent.GetDirection(i));
            if (offset < 0.5f)
                continue;
            if (!parent.SensorViews[i]) {
                Gizmos.DrawRay(parent.transform.position, parent.GetDirection(i)*parent.RayLength);
                float rayAngle = i * 360f / parent.SensorViews.Length + transform.rotation.z * Mathf.Deg2Rad;
                if (Mathf.DeltaAngle(unitDirectionAngle, rayAngle) > 0)
                    Gizmos.DrawRay(parent.transform.position + (Vector3)parent.GetDirection(i) * parent.RayLength, parent.GetNormalOfDirection(i, false) * 0.25f);
                else
                    Gizmos.DrawRay(parent.transform.position + (Vector3)parent.GetDirection(i) * parent.RayLength, parent.GetNormalOfDirection(i, true) * 0.25f);
                continue;
            }
            Gizmos.DrawLine(parent.transform.position, parent.SensorViews[i].point);
            Gizmos.DrawLine(parent.SensorViews[i].point, parent.SensorViews[i].point+GetLateralForce(i));
        }
        Gizmos.color = Color.red;
        Gizmos.DrawLine(parent.transform.position, parent.transform.position + (Vector3)body.velocity);
    }
}
