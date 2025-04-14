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
        get { 
            return parent.transform.position - parent.GetComponent<Collider2D>().offset.y * parent.transform.up; 
        }
    }
    Vector2 targetPos
    {
        get { return target.position + target.GetComponent<Collider2D>().offset.y * target.transform.up; }
    }
    public float DistanceFromTarget;
    public override void GetDirection()
    {
        if(target == null)
        {
            GetComponentInParent<SoftBody.SoftBodyUnit>().FinishedMoving(this);
            return;
        }
        if (parent == null) return;
        parent.SetArrivalModifier(SpeedupOnCharge());
        Vector2 targetPoint = GetTargetPoint();
        parent.AddSteeringForce(parent.Seek(targetPoint), priority);
    }
    Vector2 GetTargetPoint()
    {
        DistanceFromTarget = Vector2.Distance(targetPos, parent.transform.position);
        float pursueDistance = 4;
        //If near the enemy try pushing past them
        if (DistanceFromTarget < pursueDistance)
        {
            return targetPos + (targetPos - Center).normalized * 5;
        }
        //Else if you can reach the target in a straight path go to them
        else if (ReachTarget())
        {
            return targetPos;
        }
        //Else follow path to opponent
        var path = Battle.Instance.highLevelMap.A_StarSearch(parent.transform.position, targetPos);
        if (path == null) return parent.Seek(targetPos);
        for (int i = 0; i < path.Count; i++)
        {
            if (!parent.CanWalkTo(path[i])) continue;
            return path[i];
        }
        return path[path.Count - 1];
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
        if(!DrawGizmo || parent == null || !enabled || target==null) return;
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(Center, 0.1f);
        

        Gizmos.color= Color.black;
        Gizmos.DrawSphere(GetTargetPoint(), 0.2f);
        parent.GizmoSeek(GetTargetPoint());

    }
}
