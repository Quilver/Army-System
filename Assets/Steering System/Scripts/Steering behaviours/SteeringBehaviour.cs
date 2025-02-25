using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SteeringBehaviour : MonoBehaviour
{
    protected SteeringManager parent;
    protected Rigidbody2D body;
    public abstract float priority {  get; }
    
    void Start()
    {
        parent = GetComponentInParent<SteeringManager>();
        body = GetComponentInParent<Rigidbody2D>();
        GetComponentInParent<SoftBody.SoftBodyUnit>().MoveTowards += Activate;
        GetComponentInParent<SoftBody.SoftBodyUnit>().FinishedMoving += Deactivate;
        enabled = false;
    }
    [SerializeField]
    public Vector2 targetLocation;
    protected Transform target;
    public virtual void Activate(Vector2 position, Transform target)
    {
        targetLocation = position;
        this.target = target;
        enabled = true;
    }
    public virtual void Deactivate(SteeringBehaviour stopper)
    {
        target = null;
        enabled = false;
    }
    public abstract Vector2 GetDirection();
    protected float WeightedPriority(float maxDistance, float hardMinDistance, float distance)
    {
        return priority * Mathf.InverseLerp(maxDistance, hardMinDistance, distance);
    }
}
