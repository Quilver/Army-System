using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContextSteerer : MonoBehaviour
{
    [SerializeField, Range(0.1f, 3)]
    float maxSpeed;
    public float MaxSpeed
    {
        get { return maxSpeed; }
    }
    [SerializeField, Range(0.1f, 3)]
    float maxForce;
    public float MaxForce
    {
        get { return maxForce; }
    }
    [SerializeField, Range(8, 32)]
    int MovementSlots;
    [SerializeField]
    float[] priority, avoidance;
    Rigidbody2D body;
    SteeringBehaviour[] steeringBehaviours;
    public Vector2 futurePosition
    {
        get
        {
            return (Vector2)transform.position + Vector2.Max((Vector2)transform.up, body.velocity);
        }
    }
    // Start is called before the first frame update
    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        SensorViews = new RaycastHit2D[RayCount];
        priority = new float[MovementSlots];
        avoidance = new float[MovementSlots];
        steeringBehaviours = GetComponentsInChildren<SteeringBehaviour>();
    }
    // Update is called once per frame
    void Update()
    {
        UpdateSensors();
        foreach (var s in steeringBehaviours) if (s.enabled) s.GetDirection();
        body.AddForce(GetSteeringForce());//Vector2.ClampMagnitude(sumSteeringForce, maxForce));
        TurnToMovement();
        sumSteeringForce = Vector2.zero;
        for (int i = 0; i < MovementSlots; i++)
        {
            avoidance[i] = 0;
            priority[i] = 0;
        }
    }
    void TurnToMovement()
    {
        if (body.velocity.magnitude == 0) return;
        //Version 1
        //var desiredRotation = Quaternion.LookRotation(Vector3.forward, body.velocity.normalized);
        //transform.rotation = Quaternion.RotateTowards(transform.rotation, desiredRotation, 70 * Time.deltaTime);

        //Version 2
        Vector2 lookdirection = body.velocity;
        float angle = Mathf.Atan2(lookdirection.y, lookdirection.x) * Mathf.Rad2Deg - 90.0f;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }
    Vector2 sumSteeringForce;
    public void AddSteeringForce(Vector2 force)
    {
        sumSteeringForce += force;

    }
    public void AddSteeringForce(Vector2 force, float priority)
    {
        var angleOfForce = GetAngle(force.normalized);
        if (priority > 0)
            for (int i = 0; i < MovementSlots; i++)
            {
                float angle = i * 360f / RayCount + transform.rotation.z * Mathf.Deg2Rad;
                float dirPriority = priority * Vector2.Dot(force.normalized, GetDirection(i));
                if (dirPriority > this.priority[i])
                    this.priority[i] = dirPriority;
            }
        else
        {
            priority = Mathf.Abs(priority);
            for (int i = 0; i < MovementSlots; i++)
            {
                float angle = i * 360f / RayCount + transform.rotation.z * Mathf.Deg2Rad;
                float dirPriority = priority * Vector2.Dot(force.normalized, GetDirection(i));
                if (dirPriority > this.avoidance[i])
                    this.avoidance[i] = dirPriority;
            }
        }
    }
    Vector2 GetSteeringForce()
    {
        float highestPriority = float.MinValue;
        Vector2 sumForce = Vector2.zero;
        for (int i = 0; i < MovementSlots; i++)
        {

            float weight = Mathf.Max(0, priority[i] - avoidance[i]);
            if (weight <= 0) continue;
            highestPriority = weight;
            sumForce += GetDirection(i) * weight;
        }

        return sumForce.normalized * maxForce;
    }
    #region Sensor data
    public RaycastHit2D[] SensorViews;
    [SerializeField, Range(8, 24)]
    int RayCount;
    [SerializeField, Range(0.2f, 2)]
    float rayLength;
    public float RayLength{
        get { return rayLength; } 
    }
    void UpdateSensors()
    {
        for (int i = 0; i < RayCount; i++)
        {
            float angle = i * 360f / RayCount + transform.rotation.z * Mathf.Deg2Rad;
            BoxSensor(angle, i);
        }
    }
    void BoxSensor(float angle, int i, bool debugView = false)
    {
        
        var right = transform.position + Quaternion.AngleAxis(angle, transform.forward) * transform.right * transform.localScale.x * 0.5f;
        var left = transform.position - Quaternion.AngleAxis(angle, transform.forward) * transform.right* transform.localScale.x * 0.5f;
        Vector3 direction = Quaternion.AngleAxis(angle, transform.forward) * transform.up;
        float length;
        if(Mathf.Cos(angle * Mathf.Deg2Rad) > 0.5f)
            length = rayLength * Mathf.Cos(angle * Mathf.Deg2Rad);
        else length = rayLength * 0.5f;
        if (debugView)
        {
            if (SensorViews != null && SensorViews.Length !=0 && SensorViews[i])
            {
                length = SensorViews[i].distance;
                Gizmos.color = Color.red;
            } 
            else Gizmos.color = Color.blue;
            Gizmos.DrawLine(left, right);
            Gizmos.DrawLine(left, left + direction* length);
            Gizmos.DrawLine(right, right + direction * length);
            Gizmos.DrawLine(left + direction * length, right + direction * length);
        }
        else
        {
            SensorViews[i] = Physics2D.BoxCast(transform.position, transform.localScale * 0.5f, angle, direction, length - transform.localScale.y/2); 
            //Physics2D.Raycast(transform.position, direction, length);
        }

    }
    public enum GizmoDisplay
    {
        Context,
        ContextSum,
        Sensor,
        Test,
        Combo,
        None
    }
    public GizmoDisplay gizmoDisplay;
    private void OnDrawGizmos()
    {
        switch (gizmoDisplay)
        {
            case GizmoDisplay.Context:
                ContextGizmos();
                break;
            case GizmoDisplay.ContextSum:
                ContextSumGizmos();
                break;
            case GizmoDisplay.Sensor:
                SensorGizmos();
                break;
            case GizmoDisplay.Test:
                TestGizmo();
                break;
                case GizmoDisplay.Combo:
                SensorGizmos();
                ContextGizmos();
                break;
            default:
                break;
        }
    }
    void SensorGizmos()
    {
        Gizmos.color = Color.magenta;
        for (int i = 0; i < RayCount; i++)
        {
            float angle = i * 360f / RayCount + transform.rotation.z * Mathf.Deg2Rad;
            BoxSensor(angle, i, true);
        }
    }
    void TestGizmo()
    {
        Gizmos.color = Color.blue;
        for (int i = 0; i < MovementSlots; i++)
        {
            float weight = priority[i] - avoidance[i];
            if(Mathf.Abs(avoidance[i]) == 0)
                Gizmos.DrawLine(transform.position, transform.position + (Vector3)GetDirection(i) * weight);
            Gizmos.color = Color.black;
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)GetSteeringForce());
            Gizmos.DrawSphere(transform.position + 3 * (Vector3)GetSteeringForce(), 0.04f);
        }
    }
    void ContextGizmos()
    {
        if (priority == null || priority.Length != MovementSlots || steeringBehaviours==null) return;
        UpdateSensors();
        foreach (var s in steeringBehaviours) if (s.enabled) s.GetDirection();
        Gizmos.color = Color.magenta;
        for (int i = 0; i < MovementSlots; i++)
        {
            float angle = i * 360f / RayCount + transform.rotation.z * Mathf.Deg2Rad;
            Gizmos.color = Color.blue;
            var direction = Quaternion.AngleAxis(angle, transform.forward) * transform.up;
            Gizmos.DrawLine(transform.position, transform.position + direction * priority[i]);
            Gizmos.color = Color.red;
            direction = Quaternion.AngleAxis(angle+2.5f, transform.forward) * transform.up;
            Gizmos.DrawLine(transform.position, transform.position + direction * avoidance[i]);
        }
        Gizmos.color= Color.black;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)GetSteeringForce()*2);
        Gizmos.DrawSphere(transform.position + 2 * (Vector3)GetSteeringForce(), 0.04f);
        for (int i = 0; i < MovementSlots; i++)
        {
            avoidance[i] = 0;
            priority[i] = 0;
        }
    }
    void ContextSumGizmos()
    {
        if (priority == null || priority.Length != MovementSlots) return;
        UpdateSensors();
        foreach (var s in steeringBehaviours) if (s.enabled) s.GetDirection();
        Gizmos.color = Color.magenta;
        Vector2 sum =  Vector2.zero;
        for (int i = 0; i < MovementSlots; i++)
        {
            float total = priority[i] - avoidance[i];
            sum += GetDirection(i) * total;
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)GetDirection(i) * total);
        }
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)sum.normalized * 1.5f);
        Gizmos.DrawSphere(transform.position + (Vector3)sum.normalized * 1.5f, 0.1f);
        for (int i = 0; i < MovementSlots; i++)
        {
            avoidance[i] = 0;
            priority[i] = 0;
        }
    }
    #endregion
    #region Common Steering behaviours
    Vector2 GetDirection(int i)
    {
        float angle = i * 360f / RayCount + transform.rotation.z * Mathf.Deg2Rad;
        return Quaternion.AngleAxis(angle, transform.forward) * transform.up;
    }

    float GetAngle(Vector2 direction)
    {
        return 360 - (Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg * Mathf.Sign(direction.x));
    }
    public Vector2 Seek(Vector2 target)
    {
        Vector2 desiredVelocity = (target - futurePosition).normalized;
        Vector2 steerVelocity = desiredVelocity - body.velocity;
        return steerVelocity;
    }
    public Vector2 Arrive(Vector2 target)
    {
        Vector2 desiredVelocity = (target - futurePosition).normalized;
        if (Vector2.Distance(transform.position, (Vector3)target) <= 0.5f)
        {
            var distance = Vector2.Distance(transform.position, (Vector3)target);
            desiredVelocity *= Mathf.Lerp(0, MaxForce, distance / 0.5f);
        }
        Vector2 steerVelocity = Vector2.ClampMagnitude(desiredVelocity - body.velocity, MaxForce);
        return steerVelocity;
    }
    #endregion
    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log("Hit");
    }
}
