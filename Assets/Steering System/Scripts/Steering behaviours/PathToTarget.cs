using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PathToTarget : SteeringBehaviour
{
    [SerializeField, Range(0.1f, 1)]
    float _priority;
    public override float priority => _priority;
    public override void Activate(Vector2 position, Transform target)
    {
        base.Activate(position, target);
        if(target == null) enabled = false;
    }
    public override Vector2 GetDirection()
    {
        if(target == null)
        {
            GetComponentInParent<SoftBody.SoftBodyUnit>().FinishedMoving(this);
            return Vector2.zero;
        }
        if (parent == null) return Vector2.zero;
        parent.SetArrivalModifier(SpeedupOnCharge());
        if (ReachTarget())
        {
            parent.AddSteeringForce(parent.Seek(target.position), priority);
            return parent.Seek(target.position);
        }
        var path = Battle.Instance.highLevelMap.A_StarSearch(parent.transform.position, target.position);
        if (path == null) return parent.Seek(target.position);
        for (int i = 0; i < path.Count; i++)
        {
            if (!parent.CanWalkTo(path[i])) continue;
            parent.AddSteeringForce(parent.Seek(path[i]), priority);
            return parent.Seek(path[i]);
        }
        return parent.Seek(target.position);
    }
    public LayerMask SensorLayerMask;
    bool ReachTarget()
    {
        Vector2 direction = target.position - transform.position;
        float angle = Vector2.SignedAngle(Vector2.up, direction);
        float distance = direction.magnitude;
        var hit = Physics2D.BoxCast(transform.position, transform.localScale * 0.5f, 0, direction, distance, SensorLayerMask);
        return hit && hit.rigidbody.gameObject == target.gameObject;
    }
    [SerializeField, Range(1, 2.5f)]
    float ChargeSpeedBonus = 1.4f;
    float SpeedupOnCharge()
    {
        float maxDistance = 5;
        if (maxDistance > Vector2.Distance(parent.transform.position, target.position))
            return ChargeSpeedBonus;
        else
            return 1;
    }
    public bool DrawGizmo;
    public void OnDrawGizmos()
    {
        if(!DrawGizmo || parent == null || !enabled) return;
        Gizmos.color = Color.yellow;
        if (ReachTarget())
        {
            if(SpeedupOnCharge() > 1)Gizmos.color = Color.red;
            Gizmos.DrawLine(parent.transform.position, target.position);
        }
        else
        {
            bool flag = true;
            Vector2 previousPoint = target.position;
            var path = Battle.Instance.highLevelMap.A_StarSearch(parent.transform.position, target.position);
            for (int i = 1; i < path.Count; i++)
            {
                if (parent.CanWalkTo(path[i]) && flag) {
                    flag = false;
                    Gizmos.DrawLine(parent.transform.position, path[i]);
                }
                Gizmos.DrawLine(path[i-1], path[i]);
            }
        }
        

    }
}
