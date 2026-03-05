using ModelComponents;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UIElements;
namespace RangedWeapons
{
    public class Fireball : IProjectile
    {
        #region References
        Rigidbody2D body;
        #endregion
        #region Fields
        IUnit _shooter;
        [SerializeField, Range(20, 50)]
        float ProjectileSpeed;
        Vector2 _startPoint, _targetPoint;
        [SerializeField, Range(1, 20)]
        float _minDamage, _maxDamage;
        [SerializeField, Range(2, 7)]
        float _blastRadius;
        #endregion
        public override void Shoot(IUnit shooter, Vector2 targetPoint, Transform target, float damage, float accuracy)
        {
            //Assign references
            body = GetComponent<Rigidbody2D>();
            _shooter = shooter;
            //Set trajectory
            _startPoint = shooter.transform.position;
            _targetPoint = InaccurateTarget(targetPoint, accuracy);
            Vector2 direction = (_targetPoint - (Vector2)transform.position).normalized;
            body.linearVelocity = direction * Random.Range(0.9f, 1.1f) * ProjectileSpeed;
            float desiredAngle = Vector2.SignedAngle(Vector2.up, direction);
            transform.rotation = Quaternion.Euler(0, 0, desiredAngle);

        }
        void Update()
        {
            if (Vector2.Distance(transform.position, _startPoint) > Vector2.Distance(_startPoint, _targetPoint))
                Remove(false);
            else if (body.linearVelocity.magnitude < ProjectileSpeed / 2)
                Remove(false);
        }
        void Explode()
        {
            GetComponent<AudioSource>().PlayOneShot(GetComponent<AudioSource>().clip, 8);
            StartCoroutine(ExplosionAnimation());
            var hits = Physics2D.OverlapCircleAll(transform.position, _blastRadius, 1<<10);
            foreach (var hit in hits)
            {
                float damage = Random.Range(_minDamage, _maxDamage);
                var direction = hit.transform.position - transform.position;
                var multiple = Mathf.Lerp(1, 0.5f, direction.magnitude / _blastRadius);
                hit.GetComponent<ModelComponents.ITakeDamage>()?.TakeDamage(damage, direction.normalized);
            }
        }
        [SerializeField]
        Material _explosionMaterial;
        static int _explosionDistanceRef = Shader.PropertyToID("_ExplosionDistance");
        IEnumerator ExplosionAnimation()
        {
            float blastLength = 1;
            float holdLength = 0.2f;
            float radius = 1;
            var sprite = GetComponent<SpriteRenderer>();
            sprite.material = _explosionMaterial;
            var material = sprite.material;
            material.SetFloat(_explosionDistanceRef, radius);
            yield return new WaitForSeconds(holdLength);
            blastLength-= holdLength;   
            while(radius > 0)
            {
                radius-=Time.deltaTime/holdLength;
                material.SetFloat(_explosionDistanceRef, radius);
                yield return null;
            }
            sprite.enabled = false;


        }
        protected override void Remove(bool hit, float timeBeforeDestroy = 1)
        {
            Explode();
            transform.localScale = _blastRadius * Vector3.one;
            body.linearVelocity = Vector2.zero;
            enabled = false;
            base.Remove(hit, timeBeforeDestroy);
        }

        [SerializeField]
        LayerMask _shootMask;
        public override bool ValidShot(IUnit unit, Transform model, Vector2? targetPoint, Transform target)
        {
            Vector2 position = (targetPoint != null) ? targetPoint.Value : target.position;
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
            Gizmos.color = (ValidShot(unit, model, targetPoint, target)) ? Color.green : Color.red;
            Vector2 position = (targetPoint != null) ? targetPoint.Value : target.position;
            Gizmos.DrawLine(model.position, position);
        }

        public override void GizmosFireRadius(IUnit unit, Transform model, Vector2? targetPoint, Transform target, float accuracy)
        {
            Gizmos.color = Color.red;
            Vector2 position = (targetPoint != null) ? targetPoint.Value : target.position;
            Gizmos.DrawWireSphere(position, Inaccuracy(Vector2.Distance(model.position, position), accuracy));
        }
    }
}
