using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace RangedWeapons
{
    public abstract class IProjectile : MonoBehaviour
    {
        public event System.Action Hit;
        protected virtual void Remove(bool hit, float timeBeforeDestroy = 3)
        {
            if(hit) Hit?.Invoke();
            Invoke("_Delete", timeBeforeDestroy);
        }
        void _Delete()=>Destroy(gameObject);
        public abstract bool ValidShot(IUnit unit, Transform model, Vector2? targetPoint, Transform target);
        public abstract void Shoot(IUnit shooter, Vector2 targetPoint, Transform target, float damage, float accuracy);
        protected Vector2 InaccurateTarget(Vector2 targetPoint, float accuracy)
        {

            return Inaccuracy(Vector2.Distance(transform.position, targetPoint), accuracy) * Random.insideUnitCircle + targetPoint;    
        }
        public float Inaccuracy(float distance, float accuracy)
        {
            float inaccuracy = distance * 5 / accuracy + 1;
            return inaccuracy;
        }
        public abstract void GizmosValidShot(IUnit unit, Transform model, Vector2? targetPoint, Transform target);
        public abstract void GizmosFireRadius(IUnit unit, Transform model, Vector2? targetPoint, Transform target, float accuracy);
    }
}