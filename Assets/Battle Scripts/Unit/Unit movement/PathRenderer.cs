using MovementSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(LineRenderer))]
public class PathRenderer : MonoBehaviour
{
    LineRenderer lineRenderer;
    IPathfinder _pathfinder;
    IMoveOrders _moveOrder;
    // Start is called before the first frame update
    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        _pathfinder = GetComponentInParent<IPathfinder>();
        _moveOrder=GetComponentInParent<IMoveOrders>();
    }

    // Update is called once per frame
    void Update()
    {
        if(_moveOrder==null || !_moveOrder.IsMoving)
            lineRenderer.enabled = false;
        else 
            DrawPathToPosition();
    }
    void DrawPathToPosition()
    {
        lineRenderer.enabled=true;
        var path = _pathfinder.GetPath(_moveOrder.TargetPosition);
        lineRenderer.positionCount=path.Count;
        lineRenderer.SetPositions(Convert(path));
    }
    Vector3[] Convert(List<Vector2> path)
    {
        Vector3[] convertedPath = new Vector3[path.Count];
        for (int i = 0; i < path.Count; i++)
            convertedPath[i]=path[i];
        return convertedPath;
    }
}
