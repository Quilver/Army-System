using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelR : MonoBehaviour, SelectionData
{
    #region Properties
    public UnitR unit;
    Animator animator;
    Vector2Int offset;
    public Vector2Int ModelPosition
    {
        get
        {
            return GetPosition(unit.Movement.position);
        }
    }
    public Vector2Int GetPosition(PositionR position)
    {
        Vector2Int pos = position.Location;
        Vector2Int x = position.UnitDirection.Item1 * offset.x;
        Vector2Int y = position.UnitDirection.Item2 * offset.y;
        pos += x + y;
        return pos;
    }
    public bool Moving
    {
        get
        {
            Vector3 currentPosition = new Vector3(ModelPosition.x, ModelPosition.y);
            return transform.position != currentPosition;
        }
    }
    
    Tile _tile;
    public Tile TileStandingOn{
        get
        {
            TileStandingOn = Map.Instance.getTile(transform.position);
            return _tile;
        }
        private set
        {
            _tile.unit = null;
            _tile = value;
            _tile.unit = unit;
        }
    }
    #endregion
    // Start is called before the first frame update
    public void Init(Vector2Int offset, UnitR owner, int index)
    {
        this.unit = owner;
        this.offset = offset;
        transform.position = new Vector3(ModelPosition.x, ModelPosition.y);
        animator = GetComponent<Animator>();
        _tile = Map.Instance.getTile(ModelPosition);
        _tile.unit = owner;
        enabled= true;
    }
    void Update()
    {
        UpdateMovement();
    }
    void UpdateMovement()
    {
        if (unit != null && unit.state == UnitState.Fighting) { animator.Play("Attack"); }
        else if (Moving)
        {
            animator.Play("Move");
            animator.SetFloat("X", ModelPosition.x - transform.position.x);
            animator.SetFloat("Y", ModelPosition.y - transform.position.y);
            transform.position = Vector2.MoveTowards((Vector2)transform.position, ModelPosition, unit.stats.Speed* Time.deltaTime);
            var t = TileStandingOn;
        }
        else
        {
            animator.Play("Idle");
            animator.SetFloat("X", unit.Movement.position.direction.x);
            animator.SetFloat("Y", unit.Movement.position.direction.y);
        }
    }

    public Vector2Int GetPosition()
    {
        return TileStandingOn.position;
    }
}
