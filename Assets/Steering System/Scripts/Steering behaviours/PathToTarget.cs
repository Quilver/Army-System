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
    Vector2 Center {
        get { return (Vector2)parent.transform.position - parent.GetComponent<Collider2D>().offset; }
    }
    Vector2 targetPos
    {
        get { return (Vector2)target.position - target.GetComponent<Collider2D>().offset; }
    }
    public float DistanceFromTarget;
    public override Vector2 GetDirection()
    {
        if(target == null)
        {
            GetComponentInParent<SoftBody.SoftBodyUnit>().FinishedMoving(this);
            return Vector2.zero;
        }
        if (parent == null) return Vector2.zero;
        parent.SetArrivalModifier(SpeedupOnCharge());
        DistanceFromTarget = Vector2.Distance(targetPos, parent.transform.position);
        if (DistanceFromTarget < 2)
        {
            parent.AddSteeringForce(parent.Seek(targetPos + (targetPos - Center).normalized * 5), priority);
            return targetPos + (targetPos - Center).normalized * 5;
        }
        else 
        if (ReachTarget())
        {
            parent.AddSteeringForce(parent.Seek(targetPos), priority);
            return parent.Seek(targetPos);
        }
        var path = Battle.Instance.highLevelMap.A_StarSearch(parent.transform.position, targetPos);
        if (path == null) return parent.Seek(targetPos);
        for (int i = 0; i < path.Count; i++)
        {
            if (!parent.CanWalkTo(path[i])) continue;
            parent.AddSteeringForce(parent.Seek(path[i]), priority);
            return parent.Seek(path[i]);
        }
        return parent.Seek(targetPos);
    }

    public Vector2 SeekPoint()
    {
        if (DistanceFromTarget < 2) return targetPos + (targetPos - Center).normalized * 5;
        else if (ReachTarget()) return targetPos;
        var path = Battle.Instance.highLevelMap.A_StarSearch(parent.transform.position, targetPos);
        if (path == null) return targetPos;
        for (int i = 0; i < path.Count; i++)
        {
            if (!parent.CanWalkTo(path[i])) continue;
            return path[i];
        }
        return transform.position;
    }
    public LayerMask SensorLayerMask;
    public GameObject hittingOnWayToTarget;
    bool ReachTarget()
    {
        Vector2 direction = targetPos - (Vector2)transform.position;
        float angle = Vector2.SignedAngle(Vector2.up, direction);
        float distance = direction.magnitude * 1.5f;
        var hit = Physics2D.BoxCast(transform.position, transform.localScale * 0.5f, 0, direction, distance, SensorLayerMask);
        if(hit)hittingOnWayToTarget=hit.rigidbody.gameObject;
        return hit && hit.rigidbody.gameObject == target.gameObject;
    }
    [SerializeField, Range(1, 2.5f)]
    float ChargeSpeedBonus = 1.4f;
    float SpeedupOnCharge()
    {
        float maxDistance = 5;
        if (maxDistance > Vector2.Distance(parent.transform.position, targetPos))
            return ChargeSpeedBonus;
        else
            return 1;
    }
    public bool DrawGizmo;
    public void OnDrawGizmos()
    {
        if(!DrawGizmo || parent == null || !enabled) return;
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(Center, 0.1f);
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(targetPos, 0.1f);
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(targetPos + (targetPos - Center).normalized * 5, 0.1f);
        Gizmos.color = Color.yellow;
        /*
        if (ReachTarget())
        {
            if(SpeedupOnCharge() > 1)Gizmos.color = Color.red;
            Gizmos.DrawLine(parent.transform.position, targetPos);
        }
        else
        {
            bool flag = true;
            Vector2 previousPoint = targetPos;
            var path = Battle.Instance.highLevelMap.A_StarSearch(parent.transform.position, targetPos);
            for (int i = 1; i < path.Count; i++)
            {
                if (parent.CanWalkTo(path[i]) && flag) {
                    flag = false;
                    Gizmos.DrawLine(parent.transform.position, path[i]);
                }
                Gizmos.DrawLine(path[i-1], path[i]);
            }
        }*/
        Gizmos.color= Color.black;
        Gizmos.DrawSphere(SeekPoint(), 0.2f);
        Gizmos.DrawRay(parent.transform.position, (SeekPoint()- (Vector2)parent.transform.position).normalized * 5);

    }
}
