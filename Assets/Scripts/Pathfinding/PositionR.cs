using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public struct PositionR 
{
    #region Static
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
    #endregion
    #region Properties
    Vector2 location;
    public Vector2 Location
    {
        get { return location; }
        set { location = value; }
    }
    [SerializeField]
    CardinalDirections _direction;
    public Vector2Int Direction
    {
        get { return ConvertToCoordinates(_direction); }
    }
    
    public float Rotation
    {
        get
        {
            switch (_direction)
            {
                case CardinalDirections.N:
                    return 0;
                case CardinalDirections.NE:
                    return -45;
                case CardinalDirections.NW:
                    return 45;
                case CardinalDirections.W:
                    return 90;
                case CardinalDirections.SW:
                    return 135;
                case CardinalDirections.S:
                    return 180;
                case CardinalDirections.SE:
                    return -135;
                case CardinalDirections.E:
                    return -90;
            }
            Debug.LogError("Unknown Cardinal direction");
            return 0;
        }
    }
    #endregion

    public PositionR(Vector2 location, CardinalDirections direction)
    {
        this.location = location;
        this._direction = direction;
    }
    public List<PositionR> GetMoves()
    {
        List<PositionR> moves = new();
        foreach (var directions in AdjacentDirections(_direction))
        {
            moves.Add(new PositionR(location, directions));
        }
        foreach (var pos in System.Enum.GetValues(typeof(CardinalDirections)))
        {
            var p = new PositionR(location + ConvertToCoordinates((CardinalDirections)pos), _direction);
            if (p.Rotation % 2 != Rotation % 2)
                continue;
            moves.Add(p);
        }
        return moves;
    }
    public static List<Pathfinding.WeightedNode<PositionR>> GetMoves(Pathfinding.WeightedNode<PositionR> path, int advanceCost=1,
        int wheelCost = 3, int strafeCost = 12)
    {
        var nodes = path.state.GetMoves();
        List<Pathfinding.WeightedNode<PositionR>> paths = new();
        foreach (var node in nodes)
        {
            Pathfinding.WeightedNode<PositionR> newPath = new();
            newPath.state = node;
            newPath.weight = path.weight;
            if (node.location == path.state.location) newPath.weight += wheelCost;
            else if (node.location - node.Direction == path.state.location) {
                newPath.weight += advanceCost;
                newPath.state.location -= newPath.state.Direction / 2;
            } 
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
    public override string ToString()
    {
        return _direction.ToString() + " " + location;
    }
    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
    
}
