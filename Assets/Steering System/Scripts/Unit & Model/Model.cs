using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SoftBody
{
    public class Model : MonoBehaviour
    {
        Transform unit;
        [SerializeField]
        Rigidbody2D body;
        // Start is called before the first frame update
        void Start()
        {
            if (unit == null)
                body = GetComponent<Rigidbody2D>();

        }
        public void Setup(Rigidbody2D[] pins, Transform unit)
        {
            int i = 1;
            foreach (var joint in GetComponents<SpringJoint2D>())
            {
                joint.connectedBody = pins[i];
                joint.distance = Vector3.Distance(transform.position, pins[i].transform.position);
                i++;
            }
            this.unit = unit;
            body = GetComponent<Rigidbody2D>();
            ModelContainer.AddModel(this);
        }
        // Update is called once per frame
        void Update()
        {
            if (body.velocity.magnitude < 0.1f)
                transform.rotation = unit.rotation;
            else
            {
                Vector2 lookdirection = body.velocity;
                float angle = Mathf.Atan2(lookdirection.y, lookdirection.x) * Mathf.Rad2Deg - 90.0f;
                transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }
        }
    }
}

