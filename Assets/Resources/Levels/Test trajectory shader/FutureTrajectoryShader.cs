using System;
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
        int kernelID;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            trajectoryTexture = new RenderTexture(256, 256, 24);
            trajectoryTexture.format = RenderTextureFormat.ARGB32;
            trajectoryTexture.enableRandomWrite = true;
            trajectoryTexture.Create();
           
            InitBuffer();
            Sprite dynamicSprite = Sprite.Create(
        // We use Texture2D.CreateExternalTexture to treat the RT as a texture
        Texture2D.CreateExternalTexture(
            trajectoryTexture.width,
            trajectoryTexture.height,
            TextureFormat.RGBA32,
            false, false,
            trajectoryTexture.GetNativeTexturePtr()),
        new Rect(0, 0, trajectoryTexture.width, trajectoryTexture.height),
        new Vector2(0.5f, 0.5f)
    );
            GetComponent<SpriteRenderer>().sprite= dynamicSprite;
        }
        void InitBuffer()
        {
            kernelID = trajectoryComputeShader.FindKernel("ProjectionMap");
            trajectoryComputeShader.SetTexture(kernelID, "_Pixels", trajectoryTexture);
            Mesh mesh = body.GetComponent<Collider2D>().CreateMesh(false, false);
            ComputeBuffer collisionMeshBuffer = new ComputeBuffer(mesh.vertexCount, sizeof(float) * 3);
            Vector3[] vertices = mesh.vertices;
            collisionMeshBuffer.SetData(vertices);
            trajectoryComputeShader.SetBuffer(kernelID, "_CollisionMesh", collisionMeshBuffer);
            trajectoryComputeShader.SetInt("_TriangleCount", mesh.vertexCount / 3);
            trajectoryComputeShader.SetFloat("_Radius", 32);
            trajectoryComputeShader.SetFloat("_Resolution", 256);
        }
        void UpdateBuffer()
        {
            trajectoryComputeShader.SetFloat("_Time", timeStep);
            trajectoryComputeShader.SetVector("_Position", body.position);
            trajectoryComputeShader.SetVector("_Velocity", body.linearVelocity);
            trajectoryComputeShader.SetFloat("_Rotation", body.angularVelocity);
        }
        private void Update()
        {
            if (trajectoryTexture == null) return;
            UpdateBuffer();
            transform.position = body.position;
            trajectoryComputeShader.Dispatch(kernelID, trajectoryTexture.width / 8, trajectoryTexture.height / 8, 1);
        }
    }
}
