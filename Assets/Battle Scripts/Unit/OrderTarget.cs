using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(IUnit))]
public class OrderTarget : MonoBehaviour
{
    public event System.Action<Transform> TargetObject, MoveObject;
    public event System.Action<Vector2> TargetPosition, MovePosition;
    public event System.Action<Vector2, Vector2> TargetPositionAndDirection, MovePositionAndFace;
    /// <summary>
    /// the bool is there to confirm if the order is successfully completed, or is no longer valid
    /// </summary>
    public void FinishedOrder(bool successful) => CompletedOrder?.Invoke(successful);
    public event System.Action<bool> CompletedOrder;
    
    public void Halt() => Debug.Log("halt");
    //Move
    public void Move(Transform target) => MoveObject?.Invoke(target);
    public void Move(Vector2 position) => MovePosition?.Invoke(position);
    public void Move(Vector2 position, Vector2 direction) => MovePositionAndFace?.Invoke(position, direction);
    //Target
    public void Order(Transform target) =>TargetObject?.Invoke(target);
    public void Order(Vector2 position)=>TargetPosition?.Invoke(position);
    public void Order(Vector2 position, Vector2 direction) => TargetPositionAndDirection?.Invoke(position, direction);
}
