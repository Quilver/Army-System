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
    }

    // Update is called once per frame
    void Update()
    {
        //parent.AddSteeringForce(GetDirection());

    }
    public abstract Vector2 GetDirection();
    protected float WeightedPriority(float maxDistance, float hardMinDistance, float distance)
    {
        return priority * Mathf.InverseLerp(maxDistance, hardMinDistance, distance);
    }
}
