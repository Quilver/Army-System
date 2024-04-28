using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelR : MonoBehaviour//, SelectionData
{
    #region Properties
    public UnitR unit;
    public bool STOPPED = false;
    Animator animator;
    [SerializeField]
    Vector2Int offset;
    public Vector2 ModelPosition
    {
        get
        {
            return GetPosition(unit.Movement.position);
        }
    }
    Vector2 GetPosition(PositionR position)
    {
        Vector3 delta = new(offset.x, -offset.y);
        Vector2 rotatedPosition = Quaternion.Euler(0, 0, position.Rotation) * delta;
        return rotatedPosition+position.Location;
    }
    public bool Moving
    {
        get
        {
            Vector3 currentPosition = new(ModelPosition.x, ModelPosition.y);
            return transform.position != currentPosition;
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
        enabled= true;
    }
    void Update()
    {
        if(!STOPPED)
            UpdateMovement();
    }
    void UpdateMovement()
    {
        if (Moving)
        {
            animator.Play("Move");
            animator.SetFloat("X", ModelPosition.x - transform.position.x);
            animator.SetFloat("Y", ModelPosition.y - transform.position.y);
            transform.position = Vector2.MoveTowards((Vector2)transform.position, ModelPosition, unit.StatsR.Speed* Time.deltaTime);
        }
        else if (unit != null && unit.State == UnitState.Fighting) { animator.Play("Attack"); }
        else
        {
            animator.Play("Idle");
            animator.SetFloat("X", unit.Movement.position.Direction.x);
            animator.SetFloat("Y", unit.Movement.position.Direction.y);
        }
    }
}
