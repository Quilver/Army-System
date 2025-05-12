using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace RangedWeapons
{
    public abstract class IProjectile : MonoBehaviour
    {
        public abstract bool ValidShot(IUnit unit, Transform model, Vector2? targetPoint, Transform target);
        public abstract void Shoot(Vector2 targetPoint, IUnit unit);
        protected Vector2 InaccurateTarget(Vector2 targetPoint)
        {
            float distance = Vector2.Distance(transform.position, targetPoint);
            float variance = distance / 10;
            return variance * Random.insideUnitCircle + targetPoint;    
        }

    }
}