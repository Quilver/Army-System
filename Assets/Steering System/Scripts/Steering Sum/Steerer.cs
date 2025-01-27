using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Steerer : MonoBehaviour
{
    [SerializeField, Range(0.1f, 3)]
    float maxForce;
    [SerializeField]
    Transform target;
    // Start is called before the first frame update
    void Start()
    {
        body = GetComponent<Rigidbody2D>();
    }
    Rigidbody2D body;
    // Update is called once per frame
    void Update()
    {
        body.AddForce(SumSteering());
        TurnTorwardsMovement();
        //transform.rotation = Quaternion.LookRotation(Vector3.forward, body.velocity.normalized);
    }
    void TurnTorwardsMovement()
    {
        float maxTurnSpeed = 20 * maxForce;
        float turnSpeed = body.angularVelocity;
        float desiredAngle = Vector2.SignedAngle(transform.up, target.position - transform.position);
        desiredAngle = Mathf.Clamp(desiredAngle-turnSpeed, -maxTurnSpeed, maxTurnSpeed);
        float arrivalAngle = 30;
        if (Mathf.Abs(desiredAngle) < arrivalAngle)
        {
            maxTurnSpeed = Mathf.Lerp(0, maxTurnSpeed, Mathf.Abs(desiredAngle) / arrivalAngle);
        }
        if (desiredAngle == 0) { }
        else if (desiredAngle < 0)
            body.AddTorque(-maxTurnSpeed);
        else body.AddTorque(maxTurnSpeed);
    }
    Vector2 SumSteering()
    {
        speed = body.velocity.magnitude;
        Vector2 steer = Vector2.zero;
        //steer += AvoidObstacles() * 3;
        steer += Seek(target.position);
        return Vector2.ClampMagnitude(Arrive(target.position), maxForce);
    }
    [SerializeField]
    float speed;
    #region Steering Behaviours
    Vector2 Seek(Vector2 target)
    {
        Vector2 desiredVelocity = ((Vector3)target- transform.position).normalized * 10 * maxForce;
        Vector2 steerVelocity = desiredVelocity - body.velocity;
        return steerVelocity;
    }
    [SerializeField, Range (0.4f, 3)]
    float arrivalDistance = 3;
    Vector2 Arrive(Vector2 target)
    {
        Vector2 desiredVelocity = ((Vector3)target - transform.position).normalized * 10 * maxForce;
        if (Vector2.Distance(transform.position, (Vector3)target) <= arrivalDistance)
        {
            var distance = Vector2.Distance(transform.position, (Vector3)target);
            desiredVelocity *= Mathf.Lerp(0, maxForce, distance / arrivalDistance);
        }
        Vector2 steerVelocity = Vector2.ClampMagnitude(desiredVelocity - body.velocity.normalized, maxForce);
        return steerVelocity;
    }
    Vector2 AvoidObstacles()
    {
        Vector2 force = SensorRayAvoidance(transform.position);
        if (force.magnitude < SensorRayAvoidance(RightSensor).magnitude) 
            force = SensorRayAvoidance(RightSensor);
        if (force.magnitude < SensorRayAvoidance(LeftSensor).magnitude)
            force = SensorRayAvoidance(LeftSensor);
        return force;
    }
    //Path following
    /*
    [SerializeField]
    Transform pathStart;
    [SerializeField]
    float pathDivergenceRadiance = 1;
    Vector2 FollowPath()
    {
        //Get Future point
        Vector2 futurePoint = (Vector2)transform.position+body.velocity;
        //Is that point on the path
        Vector2 nearestPointOnPath = NearestPointOnLine(pathStart.position, target.position, futurePoint);
        nearestPointOnPath += (Vector2)(target.position - pathStart.position).normalized * 3;
        //Find closest point to the path
        
        if (Vector2.Distance(futurePoint, (Vector2)target.position) < Vector2.Distance(futurePoint, nearestPointOnPath))
            return Arrive(target.position);
        else if (Vector2.Distance(futurePoint, nearestPointOnPath) < pathDivergenceRadiance)
            return Vector2.zero;
        return Seek(nearestPointOnPath);;
        //Move along the path a little
    }
    */
    #endregion
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        //draw forward rays
        /*
        Gizmos.DrawLine(transform.position, transform.position + transform.up* RaySensorLength);
        DrawOverShoot(transform.position);
        Gizmos.DrawLine(RightSensor, RightSensor + transform.up * RaySensorLength);
        DrawOverShoot(RightSensor);
        Gizmos.DrawLine(LeftSensor, LeftSensor + transform.up * RaySensorLength);
        DrawOverShoot(LeftSensor);
        */
        if(body == null)return;
        Gizmos.color= Color.red;
        Vector2 ahead = transform.position;
    }
    void DrawOverShoot(Vector2 sensorOrigin)
    {
        var ray = Physics2D.Raycast(sensorOrigin, transform.up, RaySensorLength);
        if(ray.rigidbody == null) return;
        Gizmos.DrawLine(ray.point, ray.point + ray.normal/ray.fraction);
    }
    #region Sensors
    float RaySensorLength
    {
        get
        {
            float distance = 1;
            if (body != null)
            {
                distance = body.velocity.magnitude;
                distance = Mathf.Max(distance, 1);
            }
            return distance;
        }
    }
    Vector3 RightSensor
    {
        get
        {
            return transform.position + transform.right * transform.localScale.x / 2;
        }
    }
    Vector3 LeftSensor
    {
        get
        {
            return transform.position - transform.right * transform.localScale.x / 2;
        }
    }
    #endregion
    Vector2 NearestPointOnLine(Vector2 startLine, Vector2 endLine, Vector2 point)
    {
        var ap = point - startLine;
        var ab = (endLine - startLine).normalized;
        ab *= Vector2.Dot(ap, ab);
        var normalPoint = startLine + ab;
        return normalPoint;
    }
    Vector2 SensorRayAvoidance(Vector2 sensorOrigin)
    {
        var ray = Physics2D.Raycast(sensorOrigin, transform.up, RaySensorLength);
        if(ray.rigidbody == null) return Vector2.zero;
        Vector2 overshoot = sensorOrigin+ ((Vector2)transform.up*RaySensorLength)- ray.point;
        return ray.normal / ray.fraction;
        
    }
}
