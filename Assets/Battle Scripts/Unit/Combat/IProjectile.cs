using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace RangedWeapons
{
    public abstract class IProjectile : MonoBehaviour
    {
        public abstract void Shoot(Vector2 targetPoint, IUnit unit);

    }
}