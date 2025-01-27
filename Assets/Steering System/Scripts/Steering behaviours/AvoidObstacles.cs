using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AvoidObstacles : SteeringBehaviour
{
    
    [SerializeField, Range(0.1f, 1)]
    float _priority;
    public override float priority => _priority;
    public override Vector2 GetDirection()
    {
        
        return Vector2.zero;
    }




    void BoxSensor(float angle, Transform transform)
    {
        var right = transform.position + Quaternion.AngleAxis(angle, transform.forward) * transform.right * transform.localScale.x * 0.5f;
        var left = transform.position - Quaternion.AngleAxis(angle, transform.forward) * transform.right * transform.localScale.x * 0.5f;
        Vector3 direction = Quaternion.AngleAxis(angle, transform.forward) * transform.up;
        float length = parent.RayLength;
        var view= Physics2D.BoxCast(transform.position, transform.localScale * 0.5f, angle, direction, length - transform.localScale.y / 2);

    }
    static float Angle(Vector2 vector2)
    {
        return 360 - (Mathf.Atan2(vector2.x, vector2.y) * Mathf.Rad2Deg * Mathf.Sign(vector2.x));
    }


    [SerializeField]
    bool ShowGizmos;
    private void OnDrawGizmos()
    {
        if(parent == null || !ShowGizmos) return;
        
    }
}