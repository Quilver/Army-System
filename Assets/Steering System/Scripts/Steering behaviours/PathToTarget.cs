using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PathToTarget : SteeringBehaviour
{
    [SerializeField]
    public Transform target;
    [SerializeField, Range(0.1f, 1)]
    float _priority;
    public override float priority => _priority;
    public override Vector2 GetDirection()
    {
        if(parent.CanWalkTo(target.position))
            parent.AddSteeringForce(parent.Seek(target.position), priority);
        var path = Battle.Instance.highLevelMap.A_StarSearch(parent.transform.position, target.position);
        if (path == null) parent.Seek(target.position);
        for (int i = 0; i < path.Count; i++)
        {
            if (!parent.CanWalkTo(path[i])) continue;
            parent.AddSteeringForce(parent.Seek(path[i]), priority);
            break;
        }
        return parent.Seek(target.position);
    }
    public bool DrawGizmo;
    public void OnDrawGizmos()
    {
        if(!DrawGizmo || parent == null) return;
        Gizmos.color = Color.yellow;
        if (parent.CanWalkTo(target.position))
        {
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
