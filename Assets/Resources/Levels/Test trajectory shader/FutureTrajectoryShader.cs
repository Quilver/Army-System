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
        public Vector2 circle, lineStart, velocity;
        public float _Radius, distance, p1, p2;
        private void OnDrawGizmos()
        {
            Vector2 direction = velocity.normalized;
            float radius = _Radius + velocity.magnitude * timeStep;
            Vector2 pixelOffset = circle-lineStart;
            float closestPointDist = Vector2.Dot(pixelOffset, direction);
            float distanceSquare = closestPointDist * closestPointDist - pixelOffset.sqrMagnitude;
            if (distanceSquare < radius * radius)
                Gizmos.color = Color.red;
            else Gizmos.color = Color.green;
            distance = Mathf.Sqrt(radius * radius - distanceSquare);
            p1 = closestPointDist - distance; p2= closestPointDist + distance;
            Gizmos.DrawWireSphere(circle, radius);
            Gizmos.DrawRay(lineStart, velocity.normalized * closestPointDist);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(lineStart, circle);
            Gizmos.DrawLine(circle, circle+ (lineStart+ velocity.normalized * 
                closestPointDist-circle).normalized*MathF.Sqrt(distanceSquare));
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(circle, _Radius);
            Gizmos.DrawRay(lineStart+new Vector2(0,0.05f), velocity * timeStep);
        }
    }
}
