using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathToPosition : SteeringBehaviour
{
    [SerializeField]
    public Vector2 target;
    [SerializeField, Range(0.1f, 1)]
    float _priority;
    public override float priority => _priority;
    public override Vector2 GetDirection()
    {
        if (parent.CanWalkTo(target))
            parent.AddSteeringForce(parent.Seek(target), priority);
        var path = Battle.Instance.highLevelMap.A_StarSearch(parent.transform.position, target);
        if (path == null) parent.Seek(target);
        for (int i = 0; i < path.Count; i++)
        {
            if (!parent.CanWalkTo(path[i])) continue;
            parent.AddSteeringForce(parent.Seek(path[i]), priority);
            break;
        }
        return parent.Seek(target);
    }
    public bool DrawGizmo;
    public void OnDrawGizmos()
    {
        if (!DrawGizmo || parent == null) return;
        Gizmos.color = Color.yellow;
        if (parent.CanWalkTo(target))
        {
            Gizmos.DrawLine(parent.transform.position, target);
        }
        else
        {
            bool flag = true;
            Vector2 previousPoint = target;
            var path = Battle.Instance.highLevelMap.A_StarSearch(parent.transform.position, target);
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