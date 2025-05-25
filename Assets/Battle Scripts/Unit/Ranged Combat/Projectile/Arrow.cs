using ModelComponents;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UIElements;
namespace RangedWeapons
{
    public class Arrow : IProjectile
    {
        #region References
        Rigidbody2D body;
        Collider2D col;
        #endregion
        #region Fields
        IUnit _shooter;
        [SerializeField, Range(20, 50)]
        float ProjectileSpeed;
        Vector2 _startPoint, _targetPoint;
        [SerializeField, Range(1, 20)]
        float _minDamage, _maxDamage;
        #endregion
        public override void Shoot(IUnit shooter, Vector2 targetPoint, Transform target, float damage, float accuracy)
        {
            //Assign references
            body = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
            _shooter = shooter;
            //Set trajectory
            _startPoint=shooter.transform.position;
            _targetPoint = InaccurateTarget(targetPoint, accuracy);
            Vector2 direction = (_targetPoint-(Vector2)transform.position).normalized;
            body.velocity = direction * Random.Range(0.9f, 1.1f) * ProjectileSpeed;
            float desiredAngle = Vector2.SignedAngle(Vector2.up, direction);
            transform.rotation = Quaternion.Euler(0, 0, desiredAngle);

        }
        void Update()
        {
            if(Vector2.Distance(transform.position, _startPoint) > Vector2.Distance(_startPoint, _targetPoint))
                Remove(false);
            else if(body.velocity.magnitude < ProjectileSpeed/2)
                Remove(false);
            if(col.enabled)
                return;
            else if (!col.enabled && !Physics2D.OverlapCircle(transform.position, 0.5f))
                col.enabled = true;
        }
        private void OnCollisionEnter2D(Collision2D collision)
        {
            var unit = collision.gameObject.GetComponent<ModelComponents.ITakeDamage>();
            if (unit != null) 
                unit.TakeDamage(Random.Range(_minDamage, _maxDamage));
            Remove(true);
        }
        protected override void Remove(bool hit, float timeBeforeDestroy=2)
        {
            SpriteRenderer sprite = GetComponent<SpriteRenderer>();
            if (hit) sprite.color = new(1, 1, 1, 0.7f);
            else sprite.color = new(0.7f, 0.7f, 0.7f, 0.7f);
            if (!hit)body.velocity = Vector2.zero;
            enabled = false;
            col.enabled = false;
            base.Remove(hit, timeBeforeDestroy);
        }

        [SerializeField]
        LayerMask _shootMask;
        public override bool ValidShot(IUnit unit, Transform model, Vector2? targetPoint, Transform target)
        {
            Vector2 position = (targetPoint != null)? targetPoint.Value:target.position;
            Vector2 direction = (position - (Vector2)model.position);
            float distance = direction.magnitude;
            direction = direction.normalized;
            RaycastHit2D hit = Physics2D.Raycast(model.position, direction, distance, _shootMask);
            if (hit && hit.transform != target)
                return false;
            return true;
        }

        public override void GizmosValidShot(IUnit unit, Transform model, Vector2? targetPoint, Transform target)
        {
            Gizmos.color = (ValidShot(unit, model, targetPoint, target))? Color.green : Color.red;
            Vector2 position = (targetPoint != null) ? targetPoint.Value : target.position;
            Gizmos.DrawLine(model.position, position);
        }

        public override void GizmosFireRadius(IUnit unit, Transform model, Vector2? targetPoint, Transform target, float accuracy)
        {
            Gizmos.color=Color.red;
            Vector2 position = (targetPoint != null) ? targetPoint.Value : target.position;
            Gizmos.DrawWireSphere(position, Inaccuracy(Vector2.Distance(model.position, position), accuracy));
        }
    }
}
