using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct PositionR 
{
    public static List<CardinalDirections> AdjacentDirections(CardinalDirections direction)
    {
        var directions = new List<CardinalDirections>();
        switch (direction)
        {
            case CardinalDirections.N:
                directions.Add(CardinalDirections.NE);
                directions.Add(CardinalDirections.NW);
                break;
            case CardinalDirections.NE:
                directions.Add(CardinalDirections.N);
                directions.Add(CardinalDirections.E);
                break;
            case CardinalDirections.NW:
                directions.Add(CardinalDirections.N);
                directions.Add(CardinalDirections.W);
                break;
            case CardinalDirections.W:
                directions.Add(CardinalDirections.SW);
                directions.Add(CardinalDirections.NW);
                break;
            case CardinalDirections.SW:
                directions.Add(CardinalDirections.S);
                directions.Add(CardinalDirections.W);
                break;
            case CardinalDirections.S:
                directions.Add(CardinalDirections.SE);
                directions.Add(CardinalDirections.SW);
                break;
            case CardinalDirections.SE:
                directions.Add(CardinalDirections.E);
                directions.Add(CardinalDirections.S);
                break;
            case CardinalDirections.E:
                directions.Add(CardinalDirections.NE);
                directions.Add(CardinalDirections.SE);
                break;
        }
        return directions;
    }
    public static Vector2Int ConvertToCoordinates(CardinalDirections directions)
    {
        switch (directions)
        {
            case CardinalDirections.N:
                return new Vector2Int(0, 1);
            case CardinalDirections.NE:
                return new Vector2Int(1, 1);
            case CardinalDirections.NW:
                return new Vector2Int(-1, 1);
            case CardinalDirections.W:
                return new Vector2Int(-1, 0);
            case CardinalDirections.SW:
                return new Vector2Int(-1, -1);
            case CardinalDirections.S:
                return new Vector2Int(0, -1);
            case CardinalDirections.SE:
                return new Vector2Int(1, -1);
            case CardinalDirections.E:
                return new Vector2Int(1, 0);
        }
        Debug.LogError("Unknown Cardinal direction");
        return new Vector2Int(0, 0);
    }
    Vector2Int location;
    public Vector2Int Location
    {
        get { return location; }
    }
    CardinalDirections _direction;
    public Vector2Int direction
    {
        get { return ConvertToCoordinates(_direction); }
    }
    public PositionR(Vector2Int location, CardinalDirections direction)
    {
        this.location = location;
        this._direction = direction;
    }
    public List<PositionR> GetMoves()
    {
        List<PositionR> moves = new List<PositionR>();
        foreach (var directions in AdjacentDirections(_direction))
        {
            moves.Add(new PositionR(location, directions));
        }
        foreach (var pos in System.Enum.GetValues(typeof(CardinalDirections)))
        {
            Debug.Log(pos.ToString());
            moves.Add(new PositionR(location + ConvertToCoordinates((CardinalDirections)pos), _direction));
        }
        return moves;
    }
    public static List<Pathfinding.Path<PositionR>> GetMoves(Pathfinding.Path<PositionR> path, int advanceCost=1,
        int wheelCost = 4, int strafeCost = 12)
    {
        var nodes = path.state.GetMoves();
        List<Pathfinding.Path<PositionR>> paths = new List<Pathfinding.Path<PositionR>>();
        foreach (var node in nodes)
        {
            Pathfinding.Path<PositionR> newPath = new Pathfinding.Path<PositionR>();
            newPath.state = node;
            newPath.weight = path.weight;
            if (node.location == path.state.location) newPath.weight += wheelCost;
            else if (node.location - node.direction == path.state.location) newPath.weight += advanceCost;
            else newPath.weight += strafeCost;
            paths.Add(newPath);
        }
        return paths;
    }
    public override bool Equals(object obj)
    {
        if(obj is PositionR)
        {
            PositionR positionRother = (PositionR)obj;
            return positionRother._direction == _direction && positionRother.location ==location;
        }
        return false;
    }
}
