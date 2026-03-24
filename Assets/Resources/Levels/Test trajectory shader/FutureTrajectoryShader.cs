using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
namespace ComputeShaderTest
{
    public class FutureTrajectoryShader : MonoBehaviour
    {

        public Rigidbody2D body;
        public float timeStep;

        public ProjectionShaderContext context;
        int kernelID=0;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            //trajectoryComputeShader = (ComputeShader)Instantiate(trajectoryComputeShader);
            context = new ProjectionShaderContext(body, timeStep);
            context.InitBuffer(kernelID);
            GetComponent<SpriteRenderer>().sprite= context.GetSprite();
            transform.position=body.position;
            //Invoke("Pause", timeStep);
        }
        private void Update()
        {
            context.Update(kernelID, body);
            transform.position = body.position;
        }
    }
    
}
