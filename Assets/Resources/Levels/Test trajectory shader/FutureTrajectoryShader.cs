using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
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
            context = new ProjectionShaderContext(body);
            context.projectionType.ComputeShader = (ComputeShader)Instantiate(context.projectionType.ComputeShader);
            context.InitBuffer(kernelID);
            GetComponent<SpriteRenderer>().sprite= context.GetSprite();
            transform.position=body.position;
            CollisionMapManager.instance.UpdateProjections += () => context.Update(kernelID, body, timeStep);
            //Invoke("Save", 0.1f);
        }
        private void Update()
        {
            //context.Update(kernelID, body, timeStep);
            transform.position = body.position;
        }
        
        void Save()
        {
            context.ReadInResult();
        }
        private IEnumerator ReadPixel()
        {
            // Dispatch the compute shader
            context.Update(kernelID, body, timeStep);
            ComputeBuffer outputBuffer = new ComputeBuffer(1, sizeof(float) * 4);
            // Request the data from the GPU to the CPU
            AsyncGPUReadbackRequest request = AsyncGPUReadback.Request(outputBuffer);

            // Wait for the request to complete
            while (!request.done)
            {
                yield return null;
            }

            if (request.hasError)
            {
                Debug.Log("GPU readback error detected.");
            }
            else
            {
                // Extract the color components from the output array
                float[] outputArray = request.GetData<float>().ToArray();
                Color color = new Color(outputArray[0], outputArray[1], outputArray[2], outputArray[3]);
            }
        }
    }
    
}
