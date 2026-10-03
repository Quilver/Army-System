using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
namespace AISystem
{
    [RequireComponent(typeof(ISquad), typeof(InfluenceMap))]
    public abstract class IGenerator : MonoBehaviour
    {
        protected ISquad squad;
        protected InfluenceMap map;
        protected virtual void Setup()
        {
            squad = GetComponent<ISquad>();
            map = GetComponent<InfluenceMap>();
        }
        public virtual void UpdateMap()
        {
            if(map==null || squad==null) Setup();
            map.closePositions = GenerateMoves();
            map.farPositions = GenerateRemoteMoves();
            map.relevantEnemies = SenseNearbyEnemies();
            map.shootableEnemies = SenseViewableEnemies();
            map.capturePoints = SenseNearbyCapturePoints();

        }
        public abstract List<Vector2> GenerateMoves();
        public abstract List<Vector2> GenerateRemoteMoves();
        public abstract List<IUnit> SenseNearbyEnemies();
        public abstract List<IUnit> SenseViewableEnemies();
        public abstract List<CapturePoint> SenseNearbyCapturePoints();
        
        public abstract Ranges PointRange(Vector2 point);
        public enum Ranges
        {
            Near,
            Far,
            Out
        }
        static int walkableMaskArea = 1;// is the same as 1 <<NavMesh.GetAreaFromName("Walkable")
        static float maxDisplacement = 0.35f;//Why this number? But it works
        public static bool ValidPoint(Vector2 point)
        {
            NavMeshHit hit;
            return NavMesh.SamplePosition(point, out hit, maxDisplacement, walkableMaskArea) && !Physics2D.OverlapCircle(point, 3, 1 << 6);
        }
    }
}