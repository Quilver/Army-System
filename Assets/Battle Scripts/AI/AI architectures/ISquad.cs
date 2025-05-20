using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AISystem
{
    public abstract class ISquad : MonoBehaviour
    {
        [SerializeField] bool guardPosition;
        public abstract List<IUnit> GetUnitsToOrder { get; }
        public virtual Vector2 Center
        {
            get
            {
                if(guardPosition) return transform.position;
                return MeanPos();
            }
        }
        protected Vector2 MeanPos()
        {
            Vector3 center = Vector3.zero;
            foreach (var unit in GetUnitsToOrder)
                center += unit.transform.position;
            return center / GetUnitsToOrder.Count;
        }
        public virtual Vector2 Direction
        {
            get
            {
                if (guardPosition) return transform.up;
                return MeanDir();
            }
        }
        protected Vector2 MeanDir()
        {
            Vector3 dir = Vector3.zero;
            foreach (var unit in GetUnitsToOrder)
                dir += unit.transform.up;
            return dir;
        }

        List<Vector2> _samplePoints;
        public virtual List<Vector2> SamplePoints
        {
            get
            {
                if (_samplePoints != null && _samplePoints.Count != 0) return _samplePoints;
                Random.InitState(0);
                int granularityBase = 3;
                int layerCount = 3;
                _samplePoints = new List<Vector2> { Vector2.zero};
                for(int layer = 1; layer < Mathf.RoundToInt(SoftRadius/ layerCount) +1; layer++)
                {
                    int granularity = Mathf.Clamp(granularityBase * layer, 3, 16);
                    float angleMod = Random.Range(0, 360f/granularity);
                    for (int angle = 0; angle < 360; angle += 360/granularity)
                    {
                        float dist = layer * SoftRadius / layerCount;
                        float randomAngle = angle + angleMod;
                        Vector2 dir = new Vector2(Mathf.Sin(randomAngle * Mathf.Deg2Rad), Mathf.Cos(randomAngle * Mathf.Deg2Rad));
                        _samplePoints.Add(dist*dir);
                    }
                }
                for (int layer = 1; layer < Mathf.RoundToInt((HardRadius-SoftRadius) / layerCount) + 1; layer++)
                {
                    int granularity = 12;// Mathf.Clamp(granularityBase * layer, 3, 16);
                    float angleMod = Random.Range(0, 360f / granularity);
                    for (int angle = 0; angle < 360; angle += 360 / granularity)
                    {
                        float dist = layer * (HardRadius-SoftRadius) / layerCount + SoftRadius;
                        float randomAngle = angle + angleMod;
                        Vector2 dir = new Vector2(Mathf.Sin(randomAngle * Mathf.Deg2Rad), Mathf.Cos(randomAngle * Mathf.Deg2Rad));
                        _samplePoints.Add(dist * dir);
                    }
                }
                return _samplePoints;
            }
        }


        Army _squadArmy;
        public List<IUnit> EnemiesInRadius
        {
            get
            {
                List<IUnit> enemies = new();
                if(GetUnitsToOrder.Count == 0) return enemies;
                if(_squadArmy == null) _squadArmy = GetUnitsToOrder[0].GetComponentInParent<Army>();
                var collisions = CollisionUnitsInRadius;
                foreach (var target in collisions)
                {
                    IUnit unit = target.GetComponent<IUnit>();
                    if(unit == null) continue;
                    if (_squadArmy.Enemies.Contains(unit))
                        enemies.Add(unit);
                }

                return enemies;
            }
        }
        public Collider2D[] CollisionUnitsInRadius => Physics2D.OverlapCircleAll(Center, HardRadius, 1<<6);

        public virtual float SoftRadius => 10;
        public virtual float HardRadius => 15;
        
        //Gizmo and property Viewer
        [SerializeField] Vector2 _meanPos;
        [SerializeField] float _meanDirection;
        private void OnDrawGizmosSelected()
        {
            if (GetUnitsToOrder == null || GetUnitsToOrder.Count == 0) return;
            _meanPos = MeanPos(); _meanDirection = Vector2.SignedAngle(Vector2.up, MeanDir());
            Gizmos.color = Color.white;
            Gizmos.DrawRay(Center, HardRadius * 1.5f * Direction.normalized);
            foreach (var point in SamplePoints)
                Gizmos.DrawSphere(Center + point, 0.2f);
            //Soft and hard radi
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(Center, SoftRadius);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(Center, HardRadius);
            //Show enemies
            Gizmos.color = Color.red;
            foreach (var enemy in EnemiesInRadius)
                Gizmos.DrawWireSphere(enemy.transform.position, 1);

        }
    }
}
