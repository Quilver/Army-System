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
        public abstract void Shoot(IUnit shooter, Vector2 targetPoint, Transform target, float damage);
        protected Vector2 InaccurateTarget(Vector2 targetPoint)
        {
            return Offset(transform.position, targetPoint) * Random.insideUnitCircle + targetPoint;    
        }
        protected float Offset(Vector2 start, Vector2 target)
        {
            return Vector2.Distance(start, target) / 10 + 3;
        }
        public abstract void GizmosValidShot(IUnit unit, Transform model, Vector2? targetPoint, Transform target);
        public abstract void GizmosFireRadius(IUnit unit, Transform model, Vector2? targetPoint, Transform target);
    }
}