using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using AISystem.MapEvaluator;
namespace AISystem
{
    [RequireComponent(typeof(ISquad), typeof(IGenerator))]
    public class InfluenceMap : MonoBehaviour
    {
        [SerializeReference, SubclassSelector]
        List<IEvaluate> evaluators;
        [Header("Potential moves and nearby enemies")]
        public List<Vector2> closePositions, farPositions;
        public List<IUnit> relevantEnemies, shootableEnemies;
        public List<CapturePoint> capturePoints;
        public Vector2 directionOfSquadTravel;
        [Header("Scores for the different moves")]
        public List<float> DefaultScores;
        public List<ScoreAndUnit> ShootingScores, FlankEnemiesScore;
        IGenerator generator;
        public void Setup(ISquad squad)
        {
            generator = GetComponent<IGenerator>();
            foreach (var e in evaluators)
                e.Setup(squad);
        }
        public void UpdateMapScores()
        {
            generator.UpdateMap();
            foreach (var evaluator in evaluators) 
                evaluator.EvaluateOrders(this);
        }
        static int walkableMaskArea = 1;// is the same as 1 <<NavMesh.GetAreaFromName("Walkable")
        static float maxDisplacement = 0.35f;//Why this number? But it works
        bool ValidPoint(Vector2 point)
        {
            NavMeshHit hit;
            return NavMesh.SamplePosition(point, out hit, maxDisplacement, walkableMaskArea) && !Physics2D.OverlapCircle(point, 3, 1<<6);
        }
        public void Draw()
        {
            Gizmos.color = Color.white;
            foreach (var order in closePositions) Gizmos.DrawSphere(order, 0.4f);
            foreach (var enemy in relevantEnemies) Gizmos.DrawSphere(enemy.transform.position, 0.2f);
            Gizmos.color= Color.red;
            DrawMoveScores(closePositions, FlankEnemiesScore, new(0, 0.5f));
            Gizmos.color = Color.green;
            DrawMoveScores(closePositions, DefaultScores, new(0.5f, -0.5f));
            Gizmos.color = Color.blue;
            DrawMoveScores(closePositions, ShootingScores, new(-0.5f, -0.5f), 2);
        }
        void DrawMoveScores(List<Vector2> moves, List<float> scores, Vector2 offset, float sizeMod = 1)
        {
            int index = 0;
            if(moves.Count != scores.Count) return;
            foreach (Vector2 position in moves)
            {
                Vector2 pos = position;
                pos += offset;
                if (scores[index] > 0)
                    Gizmos.DrawSphere(pos, scores[index] * sizeMod);
                index++;
            }
        }
        void DrawMoveScores(List<Vector2> moves, List<ScoreAndUnit> scores, Vector2 offset, float sizeMod = 1)
        {
            int index = 0;
            if (moves.Count != scores.Count) return;
            foreach (Vector2 order in moves)
            {
                Vector2 pos = order;
                pos += offset;
                if (scores[index].score > 0)
                {
                    Gizmos.DrawSphere(pos, scores[index].score * sizeMod);
                    Gizmos.DrawLine(pos, scores[index].closestUnit.transform.position);
                }
                index++;
            }
        }
    }
    [Serializable]
    public struct ScoreAndUnit
    {
        public IUnit closestUnit;
        public float score;
        public ScoreAndUnit(IUnit closestUnit, float score)
        {
            this.closestUnit = closestUnit;
            this.score = score;
        }
    }
}
