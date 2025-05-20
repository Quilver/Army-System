using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UIElements;
namespace AISystem
{
    public class MoveToShoot : IPossibleMoves
    {
        public
        float _sweetSpotMin, _sweetSpotMax;
        
        public
        RangedWeapon weapon;

        public override List<UnitOrder> GenerateOrders(IUnit unit)
        {
            List<UnitOrder> moves = new();
            weapon=unit.GetComponentInChildren<RangedWeapon>();
            if (weapon == null) return moves;
            _sweetSpotMin = weapon.Range* 0.5f;
            _sweetSpotMax = weapon.Range * 0.75f;
            float sweetSpot = (_sweetSpotMin + _sweetSpotMax)/2;

            return moves;
        }
        void Setup(IUnit unit)
        {
            weapon = unit.GetComponentInChildren<RangedWeapon>();
            _sweetSpotMin = weapon.Range * 0.5f;
            _sweetSpotMax = weapon.Range * 0.75f;
        }
        public override float ScoreOrder(IUnit unit, UnitOrder order)
        {
            if (order.moving && order.target != null) return 0;
            if(weapon == null) Setup(unit);

            if (order.target != null) return ScoreShootEnemy(unit, order.target, unit.transform.position);
            if (CanHitTarget(unit)) return 0;
            
            
            return ScorePoint(unit, order.position.Value);
        }
        float ScorePoint(IUnit unit, Vector2 position)
        {
            float bestScore = 0;
            float modScore = 0.6f;
            foreach (var enemy in Squad.EnemiesInRadius)
            {
                bestScore = Mathf.Max(bestScore, modScore * ScoreShootEnemy(unit, enemy.transform, position));
            }
            return bestScore;
        }
        float ScoreShootEnemy(IUnit unit, Transform enemy, Vector2 unitPosition)
        {
            if (!InRange(unitPosition, enemy)) return 0;
            var sight = unit.UnitRaycast(unitPosition, enemy.transform.position, true);
            if (!HitTarget(unit, unitPosition, enemy)) return 0f;
            else if (InSweetSpot(unitPosition, enemy.transform)) return 1;
            else return 0.5f;

        }
        bool CanHitTarget(IUnit unit)
        {
            foreach (var enemy in Squad.EnemiesInRadius)
            {
                if (HitTarget(unit, unit.transform.position, enemy.transform) && InSweetSpot(unit.transform.position, enemy.transform)) return true;
            }
            return false;
        }
        bool HitTarget(IUnit unit, Vector2 position, Transform target)
        {
            var ray = unit.UnitRaycast(position, target.transform.position - unit.transform.position, true);
            if (!ray || ray.transform == target.transform)
                return true;
            else return false;
        }
        bool InRange(Vector3 point, Transform target)
        {
            var distance = Vector3.Distance(target.position, point);
            return distance < weapon.Range;
        }
        bool InSweetSpot(Vector3 point, Transform target)
        {
            var distance = Vector3.Distance(target.position, point);
            return distance > _sweetSpotMin && distance < _sweetSpotMax;
        }
        public void OnDrawGizmos()
        {
            foreach (var enemy in Squad.EnemiesInRadius)
            {
                var unit = Squad.GetUnitsToOrder[0];
                var ray = unit.UnitRaycast(unit.transform.position, enemy.transform.position - unit.transform.position, true);
                if (HitTarget(unit, unit.transform.position, enemy.transform))
                    Gizmos.color = Color.blue;
                else Gizmos.color = Color.red;
                
                if (ray) Gizmos.DrawLine(unit.transform.position, ray.point);
                else
                    Gizmos.DrawRay(unit.transform.position, enemy.transform.position - unit.transform.position);
            }
        }
        
    }
}