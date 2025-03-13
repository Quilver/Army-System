using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class SteerTowards : SteeringManager
{
    [SerializeField, Range(0.1f, 12)]
    float maxForce;
    public float MaxForce
    {
        get { return maxForce; }
    }
    List<SoftBody.Model> models;
    float Mass
    {
        get
        {
            if(models == null) models = GetComponent<SoftBody.SoftBodyUnit>()._models;
            if(models.Count == 0) return 0;
            return models.Count * models[0].GetComponent<Rigidbody2D>().mass + 6;
        }
    }
    [SerializeField, Range(8, 32)]
    int MovementSlots;
    [SerializeField, Range(1, 6)]
    float sensorLength;
    [SerializeField]
    float[] priority, avoidance;
    public RaycastHit2D[] sensorViews;
    public override RaycastHit2D[] SensorViews => sensorViews;
    Rigidbody2D body;
    SteeringBehaviour[] steeringBehaviours;
    public override Vector2 futurePosition =>(Vector2)transform.position + Vector2.Max((Vector2)transform.up, Vector2.ClampMagnitude(body.velocity, sensorLength));
    public override float MaxSpeed => MaxForce * 10;
    public override float RayLength => sensorLength;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        sensorViews = new RaycastHit2D[MovementSlots];
        priority = new float[MovementSlots];
        avoidance = new float[MovementSlots];
        steeringBehaviours = GetComponentsInChildren<SteeringBehaviour>();
    }
    void Update()
    {
        for (int i = 0; i < MovementSlots; i++)
        {
            avoidance[i] = 0;
            priority[i] = 0;
        }
        UpdateSensors();
        foreach (var s in steeringBehaviours) if (s.enabled) s.GetDirection();
        body.AddForce(GetSteeringForce().normalized * maxForce * ArrivalModifier * Time.deltaTime * Mass * 5);
        TurnToMovement();
        
    }
    #region Movement
    public override void AddSteeringForce(Vector2 direction, float priority)
    {
        var angleOfForce = 360 - (Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg * Mathf.Sign(direction.x));
        if (priority > 0)
            for (int i = 0; i < MovementSlots; i++)
            {
                float angle = i * 360f / MovementSlots + transform.rotation.z * Mathf.Deg2Rad;
                float dirPriority = Vector2.Dot(direction.normalized, GetDirection(i))*priority;
                if (dirPriority > this.priority[i])
                    this.priority[i] = dirPriority;
            }
    }
    Vector2 GetSteeringForce()
    {
        float MaxPull = 0;
        Vector2 direction = Vector2.zero;
        int index = -1;
        Vector2 total = Vector2.zero;
        for (int i = 0; i < MovementSlots; i++)
        {
            float pull = Mathf.Max(0, priority[i]-avoidance[i]);
            total += GetDirection(i)*pull;
            
            if (pull > MaxPull)
            {
                MaxPull = pull; index = i;
                if(i == 0) direction = GetDirection(MovementSlots-1) *Mathf.Min(priority[MovementSlots - 1], avoidance[MovementSlots - 1]);
                direction = GetDirection(i) * Mathf.Min(priority[i], avoidance[i]);
                direction = GetDirection((i + 1) % MovementSlots) * Mathf.Min(priority[(i+1)%MovementSlots], avoidance[(i + 1) % MovementSlots]);
            }
            
        }
        if (index == -1) return Vector2.zero;
        return GetDirection(index);
    }
    public float currentSpeed;
    void TurnToMovement()
    {
        if (GetSteeringForce().magnitude * ArrivalModifier <= 0.01) return;
        float maxTurnSpeed = 20 * maxForce;
        float turnSpeed = body.angularVelocity;
        float desiredAngle = Vector2.SignedAngle(transform.up, GetSteeringForce());
        desiredAngle = Mathf.Clamp(desiredAngle - turnSpeed, -maxTurnSpeed, maxTurnSpeed);
        float arrivalAngle = 30;
        if (Mathf.Abs(desiredAngle) < arrivalAngle)
        {
            maxTurnSpeed = Mathf.Lerp(0, maxTurnSpeed, Mathf.Abs(desiredAngle) / arrivalAngle);
        }
        if (desiredAngle == 0) { }
        else if (desiredAngle < 0)
            body.AddTorque(-maxTurnSpeed * Time.deltaTime * Mass * 5);
        else body.AddTorque(maxTurnSpeed * Time.deltaTime * Mass * 5);
    }
    
    #endregion
    #region Sensors
    void UpdateSensors()
    {
        for (int i = 0; i < MovementSlots; i++)
        {
            float angle = i * 360f / MovementSlots + transform.rotation.z * Mathf.Deg2Rad;
            BoxSensor(angle, i);
        }
    }
    //[SerializeField]
    //LayerMask SensorLayerMask;
    void BoxSensor(float angle, int i, bool debugView = false)
    {

        var right = transform.position + Quaternion.AngleAxis(angle, transform.forward) * transform.right * transform.localScale.x * 0.5f;
        var left = transform.position - Quaternion.AngleAxis(angle, transform.forward) * transform.right * transform.localScale.x * 0.5f;
        Vector3 direction = Quaternion.AngleAxis(angle, transform.forward) * transform.up;
        float length = sensorLength;
        if (debugView)
        {
            if (SensorViews != null && SensorViews.Length != 0 && SensorViews[i])
            {
                length = SensorViews[i].distance;
                Gizmos.color = Color.red;
            }
            else Gizmos.color = Color.blue;
            Gizmos.DrawLine(left, right);
            Gizmos.DrawLine(left, left + direction * length);
            Gizmos.DrawLine(right, right + direction * length);
            Gizmos.DrawLine(left + direction * length, right + direction * length);
        }
        else
        {
            SensorViews[i] = Physics2D.BoxCast(transform.position, transform.localScale * 0.5f, angle, direction, length - transform.localScale.y / 2, SensorLayerMask);
            if (SensorViews[i])
                avoidance[i] = 1 - (sensorLength - SensorViews[i].distance)/sensorLength;
            else avoidance[i] = 0;
            //Physics2D.Raycast(transform.position, direction, length);
        }

    }
    #endregion
    #region Gizmos
    public enum GizmoDisplay
    {
        Context,
        ContextSum,
        Avoid,
        Sensor,
        None
    }
    public GizmoDisplay gizmoDisplay;
    [SerializeField]
    Transform target;
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
            case GizmoDisplay.Avoid:
                AvoidanceGizmos();
                break;
            case GizmoDisplay.Sensor:
                SensorGizmos();
                break;
            default:
                break;
        }
    }
    void SensorGizmos()
    {
        Gizmos.color = Color.magenta;
        for (int i = 0; i < MovementSlots; i++)
        {
            float angle = i * 360f / MovementSlots + transform.rotation.z * Mathf.Deg2Rad;
            BoxSensor(angle, i, true);
        }
    }
    
    void ContextGizmos()
    {
        if (priority == null || priority.Length != MovementSlots || steeringBehaviours == null) return;
        UpdateSensors();
        foreach (var s in steeringBehaviours) if (s.enabled) s.GetDirection();
        Gizmos.color = Color.magenta;
        for (int i = 0; i < MovementSlots; i++)
        {
            float angle = i * 360f / MovementSlots + transform.rotation.z * Mathf.Deg2Rad;
            Gizmos.color = Color.blue;
            var direction = Quaternion.AngleAxis(angle, transform.forward) * transform.up;
            var length = priority[i];
            if(length >= 0) 
                Gizmos.DrawLine(transform.position, transform.position + direction * length * 6);
            
        }
        Gizmos.color = Color.black;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)GetSteeringForce() * 2);
        Gizmos.DrawSphere(transform.position + 2 * (Vector3)GetSteeringForce(), 0.04f);
        
    }
    void ContextSumGizmos()
    {
        if (priority == null || priority.Length != MovementSlots || steeringBehaviours == null) return;
        UpdateSensors();
        foreach (var s in steeringBehaviours) if (s.enabled) s.GetDirection();
        Gizmos.color = Color.magenta;
        for (int i = 0; i < MovementSlots; i++)
        {
            float angle = i * 360f / MovementSlots + transform.rotation.z * Mathf.Deg2Rad;
            Gizmos.color = Color.blue;
            var direction = Quaternion.AngleAxis(angle, transform.forward) * transform.up;
            var length = priority[i] - avoidance[i];
            if (length >= 0)
                Gizmos.DrawLine(transform.position, transform.position + direction * length * 6);

        }
        Gizmos.color = Color.black;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)GetSteeringForce() * 2);
        Gizmos.DrawSphere(transform.position + 2 * (Vector3)GetSteeringForce(), 0.04f);

    }
    void AvoidanceGizmos()
    {
        if (priority == null || priority.Length != MovementSlots || steeringBehaviours == null) return;
        UpdateSensors();
        foreach (var s in steeringBehaviours) if (s.enabled) s.GetDirection();
        Gizmos.color = Color.magenta;
        for (int i = 0; i < MovementSlots; i++)
        {
            float angle = i * 360f / MovementSlots + transform.rotation.z * Mathf.Deg2Rad;
            Gizmos.color = Color.blue;
            var direction = Quaternion.AngleAxis(angle, transform.forward) * transform.up;
            var length = avoidance[i];
            if (length >= 0)
                Gizmos.DrawLine(transform.position, transform.position + direction * length * 2);

        }
        Gizmos.color = Color.black;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)GetSteeringForce() * 2);
        Gizmos.DrawSphere(transform.position + 2 * (Vector3)GetSteeringForce(), 0.04f);

    }
    #endregion
}
