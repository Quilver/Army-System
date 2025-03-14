using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
                joints[i - 1] = joint;
                i++;
            }
            this.unit = unit.GetComponent<SoftBody.SoftBodyUnit>();
            body = GetComponent<Rigidbody2D>();
            ModelContainer.AddModel(this);
            InContactWith = new();
            GetComponent<SpriteRenderer>().color = (unit.GetComponentInParent<Army>().controller == Army.Controller.Player) ? Color.blue : Color.red;
            Invoke("Attack", 10f / AttacksPerTenSeconds);
        }
        public void ResetPositionInFormation(Rigidbody2D[] pins, Vector2 position)
        {
            Debug.DrawLine(transform.position, position);
            for (int i = 0; i < 3; i++)
            {
                joints[i].distance = Vector3.Distance(position, pins[i+1].transform.position);
            }
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
            Combat();
            Movement();
        }
        void Movement()
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
        [SerializeField]
        List<Model> InContactWith;
        [SerializeField, Range(0.3f, 2)]
        float MeleeRadius = 1.2f;
        void Combat()
        {
            Cleanup();
        }
        [SerializeField, Range(2f, 20)]
        float AttacksPerTenSeconds = 15;
        void Attack()
        {
            Invoke("Attack", 10f/ AttacksPerTenSeconds);
            Cleanup();
            if(InContactWith == null || InContactWith.Count == 0) return;
            var target = InContactWith[Random.Range(0, InContactWith.Count)];
            Debug.DrawRay(transform.position, (target.transform.position - transform.position).normalized * 2);
            target.body.AddForce((target.transform.position - transform.position).normalized * 2);
            target.Hit(3, this);
        }
        void Cleanup()
        {
            //Removes models that are no longer in combat
            for (int i = InContactWith.Count - 1; i >= 0; i--)
            {
                if (InContactWith[i] == null)
                {
                    InContactWith.RemoveAt(i);
                    continue;
                }
                if (Vector2.Distance(InContactWith[i].transform.position, transform.position) < MeleeRadius)
                    continue;

                InContactWith.RemoveAt(i);


            }
        }
        private void OnCollisionEnter2D(Collision2D collision)
        {
            var collUnit = collision.gameObject.GetComponent<Model>();
            if (collUnit == null || collUnit.unit == unit) return;
            if(InContactWith.Contains(collUnit)) return;
            InContactWith.Add(collUnit);
            unit.StartFight(collUnit.unit);
            if(body.velocity.magnitude <= 1) return;
            collUnit.Hit(body.velocity.magnitude * body.mass, this);

        }
        void Hit(float Power, Model attacker)
        {
            if (GetFace(attacker) == Facing.Front)
                Power = Power/2;
            else if (GetFace(attacker) == Facing.Flank)
                Power = Power * 1.5f;
            else
                Power *= 3;
            float defenceScore = Random.Range(0f, 10f);
            if (defenceScore < Power) unit.GetComponent<UnitFormation>().Death(this);

        }
        enum Facing
        {
            Front,
            Flank,
            Rear
        }
        //Checks if it is flanked
        Facing GetFace(Model enemy)
        {
            var Angle = Vector2.SignedAngle(unit.transform.up, enemy.transform.position - transform.position);
            if (Mathf.Abs(Angle) < 45) return Facing.Front;
            else if (Mathf.Abs(Angle) < 135) return Facing.Flank;
            else return Facing.Rear;
        }
        private void OnDrawGizmos()
        {
            if (InContactWith.Count == 0) return;
            Facing facing = Facing.Front;
            foreach (var model in InContactWith)
            {
                if (model == null) continue;

                var Angle = Vector2.SignedAngle(unit.transform.up, model.transform.position - transform.position);
                if (GetFace(model) == Facing.Rear) facing = Facing.Rear;
                else if(GetFace(model) == Facing.Flank && facing == Facing.Front) facing = Facing.Flank;
            }
            if (facing == Facing.Front) Gizmos.color = Color.green;
            else if (facing == Facing.Flank) Gizmos.color = Color.yellow;
            else Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.position, 0.1f);
        }
        void ResistForce()
        {

        }
    }
}

