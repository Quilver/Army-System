using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathToPosition : SteeringBehaviour
{
    
    [SerializeField, Range(0.1f, 1)]
    float _priority;
    public override float priority => _priority;
    public override void Activate(Vector2 position, Transform target)
    {
        base.Activate(position, target);
        if(target != null) 
            enabled = false;
    }
    public override void GetDirection()
    {
        if (parent == null) return;
        parent.SetArrivalModifier(ArrivalModifier());
        if(Reached())
        {
            GetComponentInParent<SoftBody.SoftBodyUnit>().FinishedMoving(this);
            return;
        }
            
        if (parent.CanWalkTo(targetLocation))
        {
            parent.AddSteeringForce(parent.Seek(targetLocation), priority);
            return;
        }
        var path = Battle.Instance.highLevelMap.A_StarSearch(parent.transform.position, targetLocation);
        if (path == null) parent.Seek(targetLocation);
        for (int i = 0; i < path.Count; i++)
        {
            if (!parent.CanWalkTo(path[i])) continue;
            parent.AddSteeringForce(parent.Seek(path[i]), priority);
            break;
        }
    }
    public float ArrivalModifier()
    {
        float distance = 2;
        float currentDistance = Vector2.Distance(targetLocation, transform.position);
        if (currentDistance > distance) return 1;
        return Mathf.InverseLerp(0, distance, currentDistance);
    }
    public bool Reached()
    {
        if(Vector2.Distance(targetLocation, transform.position) < 1f) return true;
        else return false;
    }
    public bool DrawGizmo;
    public void OnDrawGizmos()
    {
        if (!DrawGizmo || parent == null || !enabled) return;
        Gizmos.color = Color.yellow;
        if (parent.CanWalkTo(targetLocation))
        {
            Gizmos.DrawLine(parent.transform.position, targetLocation);
        }
        else
        {
            bool flag = true;
            Vector2 previousPoint = targetLocation;
            var path = Battle.Instance.highLevelMap.A_StarSearch(parent.transform.position, targetLocation);
            for (int i = 1; i < path.Count; i++)
            {
                if (parent.CanWalkTo(path[i]) && flag)
                {
                    flag = false;
                    Gizmos.DrawLine(parent.transform.position, path[i]);
                }
                Gizmos.DrawLine(path[i - 1], path[i]);
            }
        }


    }
}