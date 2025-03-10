using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
namespace SoftBody
{
    public class Model : MonoBehaviour
    {
        SoftBody.SoftBodyUnit unit;
        [SerializeField]
        Rigidbody2D body;
        [SerializeField, Range(0.2f, 1)]
        float HoldDampRatio, MoveDampRatio;
        // Start is called before the first frame update
        void Start()
        {
            if (unit == null)
                body = GetComponent<Rigidbody2D>();

        }
        SpringJoint2D[] joints;
        public void Setup(Rigidbody2D[] pins, Transform unit)
        {
            int i = 1;
            joints = new SpringJoint2D[3];
            foreach (var joint in GetComponents<SpringJoint2D>())
            {
                joint.connectedBody = pins[i];
                joint.distance = Vector3.Distance(transform.position, pins[i].transform.position);
                joints[i-1] = joint;
                i++;
            }
            this.unit = unit.GetComponent<SoftBody.SoftBodyUnit>();
            body = GetComponent<Rigidbody2D>();
            ModelContainer.AddModel(this);
            InContactWith = new();
        }
        public void Move(bool moving)
        {
            foreach (var joint in joints)
                if (moving) joint.dampingRatio = MoveDampRatio;
                else joint.dampingRatio = HoldDampRatio;
        }
        // Update is called once per frame
        void Update()
        {
            if (body.velocity.magnitude < 0.1f || unit.unitState == UnitState.Fighting)
                transform.rotation = unit.transform.rotation;
            else
            {
                Vector2 lookdirection = body.velocity;
                float angle = Mathf.Atan2(lookdirection.y, lookdirection.x) * Mathf.Rad2Deg - 90.0f;
                transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }
        }
        List<Model> InContactWith;
        private void OnCollisionEnter2D(Collision2D collision)
        {
            var collUnit = collision.gameObject.GetComponent<Model>();
            if (collUnit == null || collUnit.unit == unit) return;
            InContactWith.Add(collUnit);
            unit.StartFight(collUnit.unit);
            
        }
        private void OnCollisionExit2D(Collision2D collision)
        {
            var collUnit = collision.gameObject.GetComponent<Model>();
            if (collUnit == null || collUnit.unit == unit) return;
            InContactWith.Remove(collUnit);

        }
        private void OnDrawGizmos()
        {
            foreach (var model in InContactWith)
            {
                var Angle = Vector2.SignedAngle(transform.up, model.transform.position - transform.position);
                if (Mathf.Abs(Angle) < 45) Gizmos.color = Color.green;
                else if (Mathf.Abs(Angle) < 135) Gizmos.color = Color.yellow;
                else Gizmos.color = Color.red;
                Gizmos.DrawSphere(model.transform.position, 0.1f);
            }
        }
        void ResistForce()
        {

        }
    }
}

