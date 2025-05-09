using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ModelComponents
{
    class HeroRegiment : MonoBehaviour, IModelFormation
    {
        FixedJoint2D joint;
        [SerializeField]
        Rigidbody2D[] pins;
        public void SetUp(Rigidbody2D[] pins)
        {
            joint = GetComponent<FixedJoint2D>();
            this.pins = pins;
            joint.connectedBody = pins[2];
            joint.enabled = true;
        }

        public void SetPosition(Vector3 position, bool warpToPoint = false)
        {
            
        }


    }
}