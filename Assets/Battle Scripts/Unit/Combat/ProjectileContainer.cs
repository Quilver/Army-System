using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace RangedWeapons
{
    public class ProjectileContainer : MonoBehaviour
    {
        static GameObject instance;
        public static void AddProjectile(Transform projectile)
        {
            if (instance == null)
                instance = new GameObject("Projectile holder");
            projectile.parent = instance.transform;
        }
    }
}
