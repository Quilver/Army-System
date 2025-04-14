using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowPath : SteeringBehaviour
{
    [SerializeField, Range(0.1f, 10f)]
    float pathDivergenceRadiance;
    [SerializeField]
    LineRenderer lineRenderer;
    List<Vector2> Waypoints;
    [SerializeField, Range(0.1f, 1)]
    float _priority;
    public override float priority => _priority;

    void Start()
    {
        parent = GetComponentInParent<SteeringManager>();
        body = GetComponentInParent<Rigidbody2D>();
        Waypoints = new();
        for (int i = 0; i < lineRenderer.positionCount; i++)
            Waypoints.Add(lineRenderer.GetPosition(i));
    }
    public bool loopingPath = true;
    public override void GetDirection()
    {
        
        //Get Future point
        Vector2 futurePoint = parent.futurePosition;
        var nearestPoint = GetTargetAlongLine(GetLine());
        if (body.velocity == Vector2.zero)
            parent.AddSteeringForce(parent.transform.up, priority);
        else
        {
            var pull = WeightedPriority(0, pathDivergenceRadiance, Vector2.Distance(futurePoint, nearestPoint));
            parent.AddSteeringForce(parent.Seek(nearestPoint), pull);
        }
    }
    //return index of the start point of nearest line on the unit
    int GetLine()
    {
        float distance = float.MaxValue;
        int lineIndex = 0;
        var PointNearLine = Vector2.zero;
        for (int i = 0; i < Waypoints.Count; i++)//
        {
            //Find point on line
            if (!loopingPath && i == Waypoints.Count - 1) break;
            Vector2 start = Waypoints[i];
            Vector2 end = Waypoints[(i + 1) % Waypoints.Count];
            PointNearLine = NearestPointOnLine(start, end, parent.transform.position);
            if (Vector2.Distance(PointNearLine, parent.transform.position) > distance) continue;
            lineIndex=i;
            distance = Vector2.Distance(PointNearLine, parent.transform.position);
        }

        var Start = Waypoints[lineIndex];
        var End = Waypoints[(lineIndex + 1) % Waypoints.Count];
        PointNearLine = NearestPointOnLine(Start, End, parent.transform.position) + 
            (End - Start).normalized * (parent.futurePosition - (Vector2)parent.transform.position).magnitude;
        float distanceFromStart = (PointNearLine-Start).magnitude;
        float distanceFromEnd = (PointNearLine - End).magnitude;
        float lineDistance = (Start - End).magnitude;
        if (distanceFromStart > lineDistance || distanceFromEnd > lineDistance)
        {
            lineIndex++;
            lineIndex%= Waypoints.Count;
        }
        return lineIndex;
    }
    Vector2 GetTargetAlongLine(int index)
    {
        Vector2 start = Waypoints[index]; 
        Vector2 end = Waypoints[(index+1)% Waypoints.Count];
        var FuturePoint = parent.futurePosition;
        var directionOfBody = body.velocity.normalized;
        var directionOfLine = (end - start).normalized;
        var PointNearLine = NearestPointOnLine(start, end, parent.futurePosition);
        return PointNearLine + directionOfLine;
    }
    Vector2 NearestPointOnLine(Vector2 startPoint, Vector2 endPoint, Vector2 point)
    {
        var startToPointDirection = point - startPoint;
        var lineDirection = (endPoint - startPoint).normalized;
        lineDirection *= Vector2.Dot(startToPointDirection, lineDirection);
        var normalPoint = startPoint + lineDirection;
        return normalPoint;
    }
    [SerializeField]
    bool drawGizmo;
    private void OnDrawGizmos()
    {
        if (!drawGizmo || parent == null) return;
        Gizmos.color = Color.cyan;
        int index = GetLine();
        Gizmos.DrawLine(Waypoints[(index) % Waypoints.Count], Waypoints[(index + 1) % Waypoints.Count]);
        Gizmos.DrawSphere(GetTargetAlongLine(index), 0.1f);
        Gizmos.DrawSphere(NearestPointOnLine(Waypoints[(index) % Waypoints.Count], Waypoints[(index + 1) % Waypoints.Count], parent.futurePosition), 0.05f);
        Gizmos.color= Color.black;
        Gizmos.DrawSphere(parent.futurePosition, 0.05f);
        Gizmos.DrawLine(parent.transform.position, parent.futurePosition);
        Gizmos.DrawLine(GetTargetAlongLine(index), parent.futurePosition);

    }
}
