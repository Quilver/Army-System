using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(LineRenderer))]
public class PathRenderer : MonoBehaviour
{
    PathToPosition toPosition;
    PathToTarget toTarget;
    LineRenderer lineRenderer;
    // Start is called before the first frame update
    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        toPosition = GetComponent<PathToPosition>();
        toTarget = GetComponent<PathToTarget>();
    }

    // Update is called once per frame
    void Update()
    {
        if(GetComponentInParent<UnitTemplate>().unitState != UnitState.Moving)
            lineRenderer.enabled = false;
        else if (toPosition != null)
            DrawPathToPosition();
        else if (toTarget != null)
            DrawPathToTarget();
        else
            lineRenderer.enabled = false;
    }
    void DrawPathToPosition()
    {
        lineRenderer.enabled=true;
        var path = toPosition.PathUsed();
        lineRenderer.positionCount=path.Count;
        lineRenderer.SetPositions(path.ToArray());
    }
    void DrawPathToTarget()
    {
        lineRenderer.enabled = true;
        var path = toTarget.PathUsed();
        lineRenderer.positionCount = path.Count;
        lineRenderer.SetPositions(path.ToArray());
    }
}
