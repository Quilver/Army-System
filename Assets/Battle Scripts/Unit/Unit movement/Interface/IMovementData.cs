using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SteeringSystem
{
    public interface IMovementData 
    {
        
        public float Mass { get; }
        public float Force { get; }

        public Vector2 Center { get; }
        public Vector2 FuturePosition { get; }
        public float MaxSpeed { get; }

    }
}