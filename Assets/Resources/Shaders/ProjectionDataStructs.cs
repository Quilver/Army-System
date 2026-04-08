using System;
using System.ComponentModel;
using System.IO;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
namespace ComputeShaderTest
{
    [Serializable]
    public struct ProjectionShaderContext
    {
        #region properties
        public IProjectionType projectionType;

        //Texture Data
        public RenderTexture trajectoryTexture;
        public int resolution, windowSize;
        #endregion
        public ProjectionShaderContext(Rigidbody2D body, int resolution = 256, int windowSize = 6)
        {
            projectionType= IProjectionType.Create(body);
            
            trajectoryTexture = new RenderTexture(resolution, resolution, 24);
            trajectoryTexture.enableRandomWrite = true;
            trajectoryTexture.Create();
            this.resolution = resolution;
            this.windowSize = windowSize;
        }
        public void InitBuffer(int kernelID)
        {
            projectionType.ComputeShader.SetTexture(kernelID, "_Pixels", trajectoryTexture);
            projectionType.ComputeShader.SetBuffer(kernelID, "_Occupancy", CollisionMapManager.instance.occupancyMap);
            projectionType.ComputeShader.SetFloat("_Resolution", resolution);
            projectionType.ComputeShader.SetFloat("_WorldUnits", windowSize);
            projectionType.ComputeShader.SetFloat("_PixelsPerUnit", CollisionMapManager.instance.resolution);
            projectionType.ComputeShader.SetInt("_GridWidth", CollisionMapManager.instance.mapSize.x);
            projectionType.ComputeShader.SetInt("_GridHeight", CollisionMapManager.instance.mapSize.y);
        }
        public Sprite GetSprite()
        {
            return Sprite.Create(

                // We use Texture2D.CreateExternalTexture to treat the RT as a texture
                Texture2D.CreateExternalTexture(
                    trajectoryTexture.width,
                    trajectoryTexture.height,
                    TextureFormat.RGBA32,
                    false, false,
                    trajectoryTexture.GetNativeTexturePtr()),
                new Rect(0, 0, trajectoryTexture.width, trajectoryTexture.height),
                new Vector2(0.5f, 0.5f),
                resolution / windowSize
            );
        }
        
        public void ReadInResult()
        {
            var map = trajectoryTexture;//occupancyMap;

            Texture2D occupancy = new Texture2D(trajectoryTexture.width, trajectoryTexture.height, GraphicsFormat.R32_UInt, TextureCreationFlags.None);
            //Graphics.CopyTexture(occupancyMap, occupancy);
            RenderTexture.active = trajectoryTexture;
            occupancy.ReadPixels(new Rect(0,0, trajectoryTexture.width, trajectoryTexture.height), 0, 0);
            occupancy.Apply();
            byte[] bytes = occupancy.EncodeToPNG();
            File.WriteAllBytes(Application.dataPath + "/Circle_occupancy.png", bytes);
            
        }
    
        void UpdateBuffer(ComputeShader computeShader, int kernelID, Rigidbody2D body, float time)
        {
            projectionType.Update();
            projectionType.ComputeShader.SetFloat("_Speed", time * body.linearVelocity.magnitude);
            projectionType.ComputeShader.SetVector("_Position", body.position);
            projectionType.ComputeShader.SetVector("_Direction", body.linearVelocity.normalized);
            projectionType.ComputeShader.SetFloat("_Rotation", body.transform.rotation.eulerAngles.z * Mathf.Deg2Rad);
            projectionType.ComputeShader.SetFloat("_AngularVelocity", body.angularVelocity * time * Mathf.Deg2Rad);
        }
        public void Update(int kernelID, Rigidbody2D body, float time)
        {
            UpdateBuffer(projectionType.ComputeShader, kernelID, body, time);
            projectionType.ComputeShader.Dispatch(kernelID, trajectoryTexture.width / 8, trajectoryTexture.height / 8, 1);
        }
    }
}
