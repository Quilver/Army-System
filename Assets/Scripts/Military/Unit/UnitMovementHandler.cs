using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEditor;
using UnityEngine;
[System.Serializable]
public class UnitMovementHandler
{
    Unit unit;
    List<Model> models;
    [SerializeField]
    CardinalDirections facing;
    public void Init(Unit unit, List<Model> models)
    {
        this.unit = unit;
        this.models = models;
        movementAI = new Pathfinding.UnitMovement(unit, this);
        moveState = UnitMoveState.Idle;
    }
    public int DistanceFromTile(Vector2Int position)
    {
        int distance = int.MaxValue;
        foreach (var model in models)
        {
            float currentDist = Vector2.Distance(position, model.position);
            if (currentDist < distance)
                distance = (int)currentDist;
        }
        return distance;
    }
    public Vector2 CardinalDirection
    {
        get
        {
            switch (facing)
            {
                case CardinalDirections.N:
                    return new Vector2(0, 1);
                case CardinalDirections.NE:
                    return new Vector2(1, 1);
                case CardinalDirections.NW:
                    return new Vector2(-1, 1);
                case CardinalDirections.W:
                    return new Vector2(-1, 0);
                case CardinalDirections.SW:
                    return new Vector2(-1, -1);
                case CardinalDirections.S:
                    return new Vector2(0, -1);
                case CardinalDirections.SE:
                    return new Vector2(1, -1);
                case CardinalDirections.E:
                    return new Vector2(1, 0);
                default:
                    Debug.LogError("Unrecognised direction");
                    return new Vector2(0, 0);
            }
        }
        set
        {
            if (new Vector2(0, 1) == value) facing = CardinalDirections.N;
            else if (new Vector2(1, 1) == value) facing = CardinalDirections.NE;
            else if (new Vector2(-1, 1) == value) facing = CardinalDirections.NW;

            else if (new Vector2(-1, -1) == value) facing = CardinalDirections.SW;
            else if (new Vector2(0, -1) == value) facing = CardinalDirections.S;
            else if (new Vector2(1, -1) == value) facing = CardinalDirections.SE;

            else if (new Vector2(1, 0) == value) facing = CardinalDirections.E;
            else if (new Vector2(-1, 0) == value) facing = CardinalDirections.W;
        }
    }
    [SerializeField]
    Vector2 destination;
    Tile wayPoint;
    public Pathfinding.UnitMovement movementAI;
    [SerializeField]
    UnitMoveState moveState;
    public void SetRotation()
    {
        if (facing == CardinalDirections.N) unit.transform.rotation = Quaternion.Euler(new Vector3(0, 0, 180));
        else if (facing == CardinalDirections.NW) unit.transform.rotation = Quaternion.Euler(new Vector3(0, 0, -135));
        else if (facing == CardinalDirections.NE) unit.transform.rotation = Quaternion.Euler(new Vector3(0, 0, 135));
        else if (facing == CardinalDirections.W) unit.transform.rotation = Quaternion.Euler(new Vector3(0, 0, -90));
        else if (facing == CardinalDirections.E) unit.transform.rotation = Quaternion.Euler(new Vector3(0, 0, 90));
        else if (facing == CardinalDirections.S) unit.transform.rotation = Quaternion.Euler(new Vector3(0, 0, 0));
        else if (facing == CardinalDirections.SW) unit.transform.rotation = Quaternion.Euler(new Vector3(0, 0, -45));
        else if (facing == CardinalDirections.SE) unit.transform.rotation = Quaternion.Euler(new Vector3(0, 0, 45));
    }
    public Vector2 CenterPoint { get { return models[models.Count / 2].position; } }
    public void UpdateMovement()
    {
        if (unit.unitState==UnitState.Fighting || movementAI.PathIsEmpty()) return;
        
        if (!movementAI.PathIsEmpty() && wayPoint == null)
        {
            movementAI.validateRoute();
            Pathfinding.Path<Pathfinding.Node<Tile>> altWayPoint = movementAI.followPath();
            wayPoint = altWayPoint.state.data;
            destination = new Vector2(wayPoint.position.x, wayPoint.position.y);
            Vector2 altDir = destination - (Vector2)unit.transform.position;
            if (altWayPoint.state.direction.normalized != altDir.normalized)
            {
                moveState = UnitMoveState.Strafe;
            }
            else
            {
                moveState = UnitMoveState.Wheel;
            }
        }
        switch (moveState)
        {
            case UnitMoveState.Walk:
                Walk();
                break;
            case UnitMoveState.Strafe:
                Strafe();
                break;
            case UnitMoveState.Wheel:
                Wheel();
                break;
            default:
                wayPoint = null;
                break;
        }
    }
    public void Wheel()
    {
        Vector2 vect = wayPoint.position - (Vector2)unit.transform.position;
        if (Mathf.Abs(vect.x) > 1) vect /= Mathf.Abs(vect.x);
        if (vect != CardinalDirection)
        {

            CardinalDirection = vect;
            foreach (Model model in models)
            {
                model.setRotation(vect);
            }
            return;
        }
        else { }
        foreach (Model model in models)
        {
            if (!model.move())
            {
                return;
            }
        }
        moveState = UnitMoveState.Walk;
    }
    public void Walk()
    {
        foreach (Model model in models)
        {
            if (model.move())
            {
                model.moveOrder(destination);
            }
        }
        unit.transform.position = Vector2.MoveTowards(unit.transform.position, destination, unit.Stats.MoveSpeed * Time.deltaTime);
        if (unit.transform.position.x == destination.x && unit.transform.position.y == destination.y)
        {
            unit.unitState = UnitState.Idle;
            moveState = UnitMoveState.Idle;
        }
    }

    public void Strafe()
    {
        foreach (Model model in models)
        {
            if (model.move())
            {
                model.moveOrder(destination);
            }
        }
        unit.transform.position = Vector2.MoveTowards(unit.transform.position, destination, unit.Stats.MoveSpeed * Time.deltaTime);// * StrafeSpeed);
        if (unit.transform.position.x == destination.x && unit.transform.position.y == destination.y)
        {
            unit.unitState = UnitState.Idle;
            moveState = UnitMoveState.Idle;
        }
    }
    public bool canMove(Pathfinding.Node<Tile> node)
    {
        foreach (Model model in models)
        {
            if (!model.MoveEdge(node))
            {
                return false;
            }
        }
        return true;
    }
    public bool canMove(Pathfinding.Edge<Tile> node)
    {
        foreach (Model model in models)
        {
            if (!model.MoveEdge(node))
            {
                return false;
            }
        }
        return true;
    }
}
