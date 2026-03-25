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
        public int steps = 10;
        public Vector2 pixelOffset=new Vector2(-1,-0.5f);
        public Vector2 RotatedPos, RotatedDir, nextPos;
        Vector2 AngularVelocity(float angle)
        {
            return new Vector2(-Mathf.Sin(angle * Mathf.Deg2Rad), Mathf.Cos(angle * Mathf.Deg2Rad));
        }
        void DrawLine(Vector2 point, bool onRect)
        {
            
            Vector2 vel = (onRect) ? body.linearVelocity*timeStep : -body.linearVelocity*timeStep;
            float angularVel = (onRect) ? body.angularVelocity*timeStep : -body.angularVelocity * timeStep;
            float angle = (onRect) ? body.transform.rotation.eulerAngles.z : -body.transform.rotation.eulerAngles.z;
            Vector2 angleVel = AngularVelocity(angle)*timeStep;

            //actual starting position and velocity
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(body.position + point, 0.1f);
            Gizmos.DrawRay(body.position + point+angleVel, vel);
            Gizmos.DrawRay(body.position + point, angleVel);

            //Actual end position
            Gizmos.color = Color.blue;
            Vector2 endpos = point + angleVel + vel;
                //Quaternion.AngleAxis(body.rotation + angularVel, Vector3.forward) * point + (Vector3)vel;
            Gizmos.DrawSphere(body.position + endpos, 0.1f);
            //Rotated starting position and velocity
            Gizmos.color = Color.yellow;
            RotatedPos = (onRect)?body.transform.rotation * point : Quaternion.Inverse(body.transform.rotation) * point;
            RotatedDir = (onRect) ? body.transform.rotation * vel : Quaternion.Inverse(body.transform.rotation) * vel;
            Gizmos.DrawWireCube(body.transform.position, body.transform.localScale);
            Gizmos.DrawSphere(body.position + RotatedPos, 0.1f);
            Gizmos.DrawRay(body.position + RotatedPos, RotatedDir);
            Gizmos.color = Color.red;
            nextPos = Quaternion.AngleAxis(body.rotation + angularVel, Vector3.forward) * (RotatedPos) +(Vector3)vel;
            Gizmos.DrawSphere(body.position + nextPos, 0.1f);
            Gizmos.DrawLine(body.position + RotatedPos, body.position + nextPos);
        }
        private void OnDrawGizmos()
        {
            DrawLine(new Vector2(0, 1), true);
            //DrawLine(new Vector2(0, -1), true);
            //DrawLine(new Vector2(-0.5f, 1), false);
            

        }
    }
    
}
