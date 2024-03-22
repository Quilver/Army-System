using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Model : MonoBehaviour, SelectionData
{
    public Vector2 position;
    Unit unit;
    //public GameObject self;
    string animState = "idle";
    Tile wayPoint;
    bool moving;
    public Vector2 destination, direction, offset, rotatedOffset;
    public void Init(int x, int y, Vector2 offset, Unit unit, int index)
    {
        this.offset= offset;
        rotatedOffset= rotate(unit.CardinalDirection);
        //setRotation(unit.CardinalDirection);
        position=(Vector2)unit.transform.position + rotatedOffset;
        this.unit= unit;
        transform.position = position;
        direction = unit.CardinalDirection;
        if (offset.x == 0 && offset.y == 0) { gameObject.name = "Banner" + index; }

    }
    void Update()
    {
        Map.Instance.getTile((int)position.x, (int)position.y).unit = null;
        movementUpdate();
        Map.Instance.getTile((int)position.x, (int)position.y).unit = unit;

    }
    #region Animation
    void setAnimation()
    {
        string facing = animState;
        getFacing();
        if (direction.y == 0) { facing += "C"; }
        else if (direction.y == -1) { facing += "S"; }
        else if (direction.y == 1) { facing += "N"; }
        if (direction.x == 0) { facing += "C"; }
        else if (direction.x == -1) { facing += "W"; }
        else if (direction.x == 1) { facing += "E"; }
        if (direction == null || direction == Vector2.zero) { facing = animState + "SC"; }
        if (gameObject.gameObject.GetComponent<Animator>() != null && facing != animState)
        {
            Animator anim = gameObject.gameObject.GetComponent<Animator>();
            anim.Play(facing, -1);
        }
    }
    void getFacing()
    {
        if (wayPoint == null)
        {
            return;
        }
        Vector2 move = destination - position;
        //just vertical (done)
        if (move.x == 0)
        {
            direction.x = 0;
            if (move.y > 0) { direction.y = 1; }//nc
            else { direction.y = -1; }//sc
        }
        //to the right
        else if (move.x > 0)
        {
            if (move.y == 0) { direction.x = 1; direction.y = 0; }//ce
            //up
            else if (move.y > 0) { direction = new Vector2(1, 1); }//ne
            //down
            else { direction = new Vector2(1, -1); }//se
        }
        //to the left
        else
        {
            if (move.y == 0) { direction.x = -1; direction.y = 0; }//cw
            //up
            else if (move.y > 0) { direction = new Vector2(-1, 1); }//nw
            //down
            else { direction = new Vector2(-1, -1); }//sw
        }
    }
    #endregion

    #region Combat
    public Unit GetEnemyUnit()
    {
        var overlaps = Physics2D.OverlapCircle(position, 0.6f);
        return null;
    }
    public void Die()
    {
        animState = "death";
        setAnimation();
        gameObject.layer = LayerMask.NameToLayer("Terrain");
        Destroy(gameObject);
        Destroy(this);
    }
    public void attack()
    {

    }
    #endregion

    //movement
    #region Movement
    public void moveOrder(Vector2 waypoint)
    {
        destination = new Vector2(waypoint.x + rotatedOffset.x, waypoint.y + rotatedOffset.y);
        //print(destination);
        moving = true;
        wayPoint = Map.Instance.getTile(waypoint + rotatedOffset);
        //print(destination + " " + wayPoint.getDetails());
        animState = "walk";
        setAnimation();
    }
    void movementUpdate()
    {
        if (wayPoint == null) { }
        else if (moving) walk();
        else
        {
            wayPoint = null;
            animState = "idle";
            setAnimation();
        }
    }
    void walk()
    {
        position = Vector2.MoveTowards((Vector2)gameObject.transform.position, destination, unit.Stats.MoveSpeed * Time.deltaTime);
        gameObject.transform.position = position;
        if (position.x == destination.x && position.y == destination.y)
        {
            moving = false;
        }
    }
    Vector2 rotate(Vector2 angle)
    {
        Vector2 newPosition = new Vector2(offset.x, offset.y);
        float degrees;
        if (angle.y == 0)
        {
            //(1, 0)
            if (angle.x > 0)
            {
                //newPosition.x *= -1;
                //newPosition.y *= -1;
                //return newPosition;
                degrees = 90;
            }
            //(-1, 0)
            else
            {
                //return newPosition;
                degrees = -90;
            }
        }
        else if (angle.y > 0)
        {
            //(0, 1) right
            if (angle.x == 0)
            {
                //return newPosition;
                degrees = 180;
            }
            //(1, 1) right
            else if (angle.x > 0)
            {
                degrees = 135;
            }
            //(-1, 1)  right
            else
            {
                degrees = -135;
            }
        }
        else
        {
            //(0, -1) right
            if (angle.x == 0)
            {
                //newPosition.x *= -1;
                //newPosition.y *= -1;
                //return newPosition;
                degrees = 0;
            }
            //(1, -1) right
            else if (angle.x > 0)
            {
                degrees = 45;
            }
            //(-1, -1) right
            else
            {
                degrees = -45;
            }
        }
        float radians = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);

        float tx = newPosition.x;
        float ty = newPosition.y;
        return new Vector2(cos * tx - sin * ty, sin * tx + cos * ty);
        //return new Vector2(Mathf.Round(cos * tx - sin * ty), Mathf.Round(sin * tx + cos * ty));
    }
    public void setRotation(Vector2 rotation)
    {
        rotatedOffset = rotate(rotation);
        moveOrder(unit.transform.position);

    }
    public bool move()
    {
        return !moving;
    }
    public bool MoveEdge(Pathfinding.Node<Tile> edge)
    {
        Vector2 edgePosition = edge.position + rotate(edge.direction);
        if (Map.Instance.getTile(edgePosition) == null) return false;
        return Map.Instance.getTile(edgePosition).Walkable(unit);
    }
    public bool MoveEdge(Pathfinding.Edge<Tile> edge)
    {
        Vector2 edgePosition = edge.node.data.position + rotate(edge.direction);
        if (Map.Instance.getTile(edgePosition) == null) return false;
        return Map.Instance.getTile(edgePosition).Walkable(unit);
    }
    #endregion


    public string GetData()
    {
        return "(Unit at x " + position.x + ", y" + position.y + ")";
    }
}
