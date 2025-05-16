using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MovementSystem;
public class MoveTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(GiveOrders());
    }
    public IMoveOrders[] units;
    public List<Rigidbody2D> bodies;
    public float startTime;
    IEnumerator GiveOrders()
    {
        bodies = new();
        units = GetComponentsInChildren<IMoveOrders>();
        foreach (var unit in units) bodies.Add(unit.GetComponent<Rigidbody2D>());
        yield return null;
        foreach (var order in units) OrderMove(order);
    }
    void OrderMove(MovementSystem.IMoveOrders move)
    {
        if(move.Target != null) 
            move.MoveTo(move.Target);
        else
            move.MoveTo(move.TargetPosition, Vector2.zero);
    }
    
}
