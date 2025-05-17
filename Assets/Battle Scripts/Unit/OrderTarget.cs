using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(IUnit))]
public class OrderTarget : MonoBehaviour
{
    public event System.Action<Transform> TargetObject;
    public event System.Action<Vector2> TargetPosition;
    public event System.Action<Vector2, Vector2> TargetPositionAndDirection;
    
    public void Order(Transform target) =>TargetObject?.Invoke(target);
    public void Order(Vector2 position)=>TargetPosition?.Invoke(position);
    public void Order(Vector2 position, Vector2 direction) => TargetPositionAndDirection?.Invoke(position, direction);

}
