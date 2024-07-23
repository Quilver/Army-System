using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelR : MonoBehaviour//, SelectionData
{
    #region Properties
    public Vector2 ModelSize = Vector2.one;
    public UnitBase unit;
    public bool STOPPED = false;
    Animator animator;
    [SerializeField]
    public Vector2 offset;
    public Vector2 ModelPosition
    {
        get
        {
            
            return GetPosition(unit.Movement.Location, unit.Movement.Rotation);
        }
    }
    Vector2 GetPosition(Vector2 position, float rotation)
    {
        Vector3 delta = new(offset.x, -offset.y);
        Vector2 rotatedPosition = Quaternion.Euler(0, 0, rotation) * delta;
        return rotatedPosition+position;
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
    public void Init(Vector2 offset, UnitBase owner, int index)
    {
        this.unit = owner;
        offset.x *= ModelSize.x;
        offset.y *= ModelSize.y;
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
            float unitSpeed = ((StatSystem.IMovementStats)unit.UnitStats).Speed;
            transform.position = Vector2.MoveTowards((Vector2)transform.position, ModelPosition, unitSpeed * Time.deltaTime);
        }
        else if (unit != null && unit.State == UnitState.Fighting) { animator.Play("Attack"); }
        else
        {
            animator.Play("Idle");
            var direction = GetVectorFromAngle(unit.Movement.Rotation);
            animator.SetFloat("X", direction.x);
            animator.SetFloat("Y", direction.y);
        }
    }
    static Vector3 GetVectorFromAngle(float angle)
    {
        float angleRad = angle * (Mathf.PI / 180f);
        return new Vector3(Mathf.Sin(angleRad), Mathf.Cos(angleRad));
    }
}
