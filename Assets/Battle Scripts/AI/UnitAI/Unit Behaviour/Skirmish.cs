using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
namespace AISystem.Behaviour
{
    [System.Serializable]
    public class Skirmish : UnitBehaviour
    {
        [SerializeField, Range(5, 15)] float retreatDistance = 10;
        [SerializeField, Range(0.3f, 1)] float sweetRange = 0.75f;
        RangedWeapon weapon;
        public override void Setup(IUnit unit, ISquad squad, InfluenceMap map)
        {
            if (unit == null) throw new System.Exception("Move to shoot ai is missing unit");
            base.Setup(unit, squad, map);
            weapon = unit.GetComponentInChildren<RangedWeapon>();

        }
        UnitOrder lastOrder;
        protected override void MakeUnitMove()
        {
            IUnit nearestEnemy = GetClosestUnit();
            if (nearestEnemy == null) return;
            //If enemy is too close retreat
            if (EnemyTooClose(nearestEnemy)) Retreat(nearestEnemy);
            //If it can shoot its target do nothing
            else if (IsOrCanShoot()) return;
            //If it needs to move and is not moving to shoot at a target, do that
            else MoveToShoot(nearestEnemy);
        }

        bool EnemyTooClose(IUnit nearestEnemy) => Vector2.Distance(unit.transform.position, nearestEnemy.transform.position) < retreatDistance;
        void Retreat(IUnit nearestEnemy)
        {
            lastOrder = new(squad.Center);
            lastOrder.MakeOrder(unit);
        }
        IUnit enemyToShoot;
        bool IsOrCanShoot()
        {
            return weapon.Targets.Count > 0;
        }
        void ShootEnemy()
        {
            if (weapon.CurrentTarget != null) return;
            lastOrder = new(weapon.Targets[0].target.transform, false);
        }
        void MoveToShoot(IUnit nearestEnemy)
        {
            lastOrder = new(MoveToShootPos(nearestEnemy), nearestEnemy.transform.position);
            lastOrder.MakeOrder(unit);
        }
        Vector2 MoveToShootPos(IUnit nearestEnemy)
        {
            float sweetDistance = weapon.Range * sweetRange;
            Vector2 directionToEnemy = (Vector2)nearestEnemy.transform.position - squad.Center;
            if(directionToEnemy.magnitude < sweetDistance) return squad.Center;
            var sweetPoint = squad.Center + (directionToEnemy.magnitude - sweetDistance) * directionToEnemy.normalized;
            return sweetPoint;
        }

        IUnit GetClosestUnit()
        {
            float distance = float.PositiveInfinity;
            IUnit closestEnemy = null;
            foreach (var enemy in map.relevantEnemies)
            {
                if (enemy == null) continue;
                float pathDistance = UnitDistance(enemy.transform);
                if (distance < pathDistance) continue;
                distance = pathDistance;
                closestEnemy = enemy;

            }
            return closestEnemy;
        }
        float UnitDistance(Transform enemy)
        {

            NavMeshPath path = new();
            if (!NavMesh.CalculatePath(unit.transform.position, enemy.position, MovementSystem.NavPathfinder.MASK, path))
                return float.PositiveInfinity;
            float distance = 0;
            for (int i = 1; i < path.corners.Length; i++)
                distance += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            return distance;
        }
        public override void DrawDebug()
        {
            var enemy = GetClosestUnit();
            if(enemy == null) return;
            Gizmos.DrawWireSphere(unit.transform.position, weapon.Range * sweetRange);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(unit.transform.position, retreatDistance);
            if (EnemyTooClose(enemy))
                Gizmos.DrawLine(unit.transform.position, squad.Center);
            Gizmos.color = Color.red;
            Gizmos.DrawLine(enemy.transform.position, unit.transform.position);
            Gizmos.DrawCube(enemy.transform.position, Vector3.one);
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(MoveToShootPos(enemy), 1);
        }
    }
}