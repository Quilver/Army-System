using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace RangedWeapons
{
    [CreateAssetMenu(menuName = "Stats/Ranged weapon")]
    public class IRangedWeaponData : ScriptableObject
    {
        public IProjectile _projectile;
        [Range(5, 40)]
        public float range;
        [Range(3, 20)]
        public float ReloadTimeBase;
        [Range(3, 20)]
        public float AccuracyBase;
        [Range(0, 5)]
        public float MinDamage;
        [Range(3, 20)]
        public float MaxDamage;
        [Range(0, 360)]
        public float FieldOfView;
        public TargetType TargetSystem;
        public enum TargetType
        {
            Enemy, 
            Location,
            Ally,
            EnemyAndLocation,
            AllyAndLocation
        }
    }
}