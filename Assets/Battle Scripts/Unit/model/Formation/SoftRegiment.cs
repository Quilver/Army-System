using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ModelComponents
{
    class SoftRegiment : MonoBehaviour, IModelFormation
    {
        [SerializeField, Range(0.2f, 1)]
        float HoldDampRatio, MoveDampRatio;
        SpringJoint2D[] joints;
        Rigidbody2D[] pins;
        public void SetUp(Rigidbody2D[] pins, Vector2 offsetPos, Transform unit)
        {
            joints = new SpringJoint2D[3];
            this.pins = pins;
            int i = 0;
            foreach (var joint in GetComponents<SpringJoint2D>())
            {
                joint.connectedBody = pins[i];
                joint.distance = Vector3.Distance(transform.position, pins[i].transform.position);
                joints[i] = joint;
                i++;
            }

        }
        public void SetPosition(Vector3 position, Vector2 offsetPos, bool warpToPoint = false)
        {
            if(warpToPoint) transform.position = position;
            int i = 0;
            foreach (var joint in GetComponents<SpringJoint2D>())
            {
                joint.connectedBody = pins[i];
                joint.distance = Vector3.Distance(position, pins[i].transform.position);
                joints[i] = joint;
                i++;
            }
        }

        
    }
}