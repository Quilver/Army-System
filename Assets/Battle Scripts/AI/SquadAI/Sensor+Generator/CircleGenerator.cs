using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AISystem.Generator
{
    public class CircleGenerator : IGenerator
    {
        [SerializeField, Range(10, 40)]
        float shortRadius, farRadius;
        [SerializeField, Range(2, 5)] float distancePerPoint = 4;
        public override List<Vector2> GenerateMoves()
        {
            if(squad == null)squad = GetComponent<ISquad>();
            List<Vector2> moves = new List<Vector2>();
            moves.Add(squad.Center);
            for(float radius = distancePerPoint; radius <= shortRadius; radius += distancePerPoint)
            {
                GenerateRing(moves, radius);
            }
            return moves;
        }
        public override List<Vector2> GenerateRemoteMoves()
        {
            if (squad == null) squad = GetComponent<ISquad>();
            List<Vector2> moves = new List<Vector2>();
            if(shortRadius > farRadius) return moves;
            for (float radius = shortRadius; radius <= farRadius; radius += distancePerPoint)
            {
                GenerateRing(moves, radius);
            }
            return moves;
        }
        void GenerateRing(List<Vector2> moves, float radius)
        {
            float angleBoost = 360/radius;
            for (float angle = 0; angle < 360; angle += angleBoost)
            {
                Vector2 pos = (Vector2)(Quaternion.AngleAxis(angle, transform.forward) * (radius * Vector2.up)) +squad.Center;
                if(ValidPoint(pos))
                    moves.Add(pos);
            }
        }
        public override Ranges PointRange(Vector2 point)
        {
            float distance = Vector2.Distance(point, squad.Center);
            if (distance < shortRadius) return Ranges.Near;
            else if(distance < farRadius) return Ranges.Far;
            else return Ranges.Out;
        }

        public override List<IUnit> SenseNearbyEnemies()
        {
            List<IUnit> enemies = new();
            List<IUnit> nearEnemies = new();
            if (squad.SquadArmy == null) return enemies;
            var collisions = Physics2D.OverlapCircleAll(squad.Center, farRadius, 1 << 6);
            foreach (var target in collisions)
            {
                IUnit unit = target.GetComponent<IUnit>();
                if (unit == null) continue;
                if (!squad.SquadArmy.Enemies.Contains(unit))continue;
                enemies.Add(unit);
                if(Vector2.Distance(unit.transform.position, squad.Center) < shortRadius) nearEnemies.Add(unit);
            }
            map.closeEnemies = nearEnemies;
            return enemies;
        }
        public override List<CapturePoint> SenseNearbyCapturePoints()
        {
            List<CapturePoint> capturePoints = new List<CapturePoint>();
            var collisions = Physics2D.OverlapCircleAll(squad.Center, farRadius, 1 << 13);
            foreach (var target in collisions)
            {
                CapturePoint capturePoint = target.GetComponent<CapturePoint>();
                if (capturePoint == null) continue;
                capturePoints.Add(capturePoint);
            }
            return capturePoints;
        }
        public override List<IUnit> SenseViewableEnemies()
        {
            if (squad.SquadArmy == null) return new List<IUnit>();
            return squad.SquadArmy.Enemies;
        }
        [SerializeField] bool DrawGizmo;
        private void OnDrawGizmosSelected()
        {
            if (!DrawGizmo) return;
            foreach (var point in GenerateMoves())
                Gizmos.DrawSphere(point, 0.5f);
            Gizmos.DrawWireSphere(squad.Center, shortRadius);
            Gizmos.color = Color.black;
            foreach (var point in GenerateRemoteMoves())
                Gizmos.DrawSphere(point, 0.5f);
            Gizmos.DrawWireSphere(squad.Center, farRadius);
            Gizmos.color = Color.red;
            foreach (var enemy in SenseNearbyEnemies())
            {
                Gizmos.DrawSphere(enemy.transform.position, 0.5f);
            }
        }
    }
}