using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AvoidWalls : SteeringBehaviour
{
    [SerializeField, Range(0.5f, 3)]
    float maxDistance = 1f;
    [SerializeField, Range(0.2f, 1.5f)]
    float HardMin = 0.5f;
    [SerializeField, Range(0.1f, 2.5f)]
    float _priority;
    public override float priority => _priority;

    public override Vector2 GetDirection()
    {
        Vector2 sumForce = Vector2.zero;
        foreach (var sensor in parent.SensorViews)
        {
            if (sensor)
            {
                if (sensor.distance > maxDistance) continue;

                Debug.DrawLine(parent.transform.position, parent.transform.position + (Vector3)sensor.point - parent.transform.position, Color.green);
                float p = Mathf.InverseLerp(maxDistance, HardMin, sensor.distance);
                //Debug.Log("inverse lerp:"+maxDistance+", "+HardMin+", "+sensor.distance+"->"+p);
                parent.AddSteeringForce(sensor.point - (Vector2)parent.transform.position, -priority * p);
                //parent.AddSteeringForce(-parent.Seek(sensor.point), (1 - sensor.fraction));
            }
        }
        return sumForce;
    }
}
