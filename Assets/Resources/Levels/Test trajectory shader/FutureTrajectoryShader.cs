using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ComputeShaderTest
{
    public class FutureTrajectoryShader : MonoBehaviour
    {

        public Rigidbody2D body;
        public float timeStep = 2f;

        //Compute shader variables
        public ComputeShader trajectoryComputeShader;
        public RenderTexture trajectoryTexture;
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
            trajectoryTexture = new RenderTexture(256, 256, 4);
            trajectoryTexture.enableRandomWrite = true;
            trajectoryTexture.Create();

            InitBuffer();
        }
        void InitBuffer()
        {
            trajectoryComputeShader.SetTexture(0, "Pixels", trajectoryTexture);
            Mesh mesh = body.GetComponent<Collider2D>().CreateMesh(false, false);
            //trajectoryComputeShader.SetBuffer(0, "objectMesh", mesh.vertexBufferTarget);
            trajectoryComputeShader.SetFloat("Radius", 1);
            trajectoryComputeShader.SetFloat("Resolution", 1);
        }
        void UpdateBuffer()
        {
            trajectoryComputeShader.SetFloat("timeStep", timeStep);
            trajectoryComputeShader.SetVector("Position", body.position);
            trajectoryComputeShader.SetVector("Velocity", body.linearVelocity);
            trajectoryComputeShader.SetFloat("Rotation", body.angularVelocity);
        }
        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if(trajectoryTexture == null) return;
            UpdateBuffer();
            trajectoryComputeShader.Dispatch(0, trajectoryTexture.width / 8, trajectoryTexture.height / 8, 1);
            Graphics.Blit(trajectoryTexture, destination);
        }
    }
}
