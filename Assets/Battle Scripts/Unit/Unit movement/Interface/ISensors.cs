using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SteeringSystem
{
    public interface Sensors
    {
        public float SensorLength { get; }
        public List<RaycastHit2D> Sensors { get; }
    }
}