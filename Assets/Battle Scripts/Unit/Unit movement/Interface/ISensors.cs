using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MovementSystem
{
    public interface Sensors
    {
        public float SensorLength { get; }
        public List<RaycastHit2D> Sensors { get; }
        public Vector2 SensorDirection(RaycastHit2D hit);
    }
}