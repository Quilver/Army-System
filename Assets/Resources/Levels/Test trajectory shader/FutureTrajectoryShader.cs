using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ComputeShader
{
    public class FutureTrajectoryShader : MonoBehaviour
    {

        public List<Rigidbody2D> staticBodies, dynamicBodies;
        public float timeStep = 2f;
        struct ObjectData
        {
            public Vector2 position;
            public Vector2 velocity;
            public float angularVelocity;
            public float radius; // Based on collider size
        }
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
