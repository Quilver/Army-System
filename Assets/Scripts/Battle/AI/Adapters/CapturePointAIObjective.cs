using UnityEngine;

namespace LeveragedWars.Battle.AI
{
    /// <summary>
    /// The AI's view of a CapturePoint (IAIObjective). Add it to a capture point to give the point an id such as
    /// "Primary"; BattleAIBattlefield adds one with the GameObject's name as the id to any capture point without one.
    /// </summary>
    [DisallowMultipleComponent, RequireComponent(typeof(CapturePoint))]
    public sealed class CapturePointAIObjective : MonoBehaviour, IAIObjective
    {
        [SerializeField, Tooltip("Id used by formations and triggers, e.g. Primary. Empty = the GameObject's name.")]
        string id;

        CapturePoint point;

        CapturePoint Point
        {
            get
            {
                if (point == null && this != null) point = GetComponent<CapturePoint>();
                return point;
            }
        }

        public static CapturePointAIObjective For(CapturePoint point)
        {
            if (point == null) return null;
            return point.TryGetComponent(out CapturePointAIObjective objective)
                ? objective
                : point.gameObject.AddComponent<CapturePointAIObjective>();
        }

        public string Id => string.IsNullOrWhiteSpace(id) ? name : id.Trim();
        public Vector3 Position => transform.position;
        public float Radius => Point != null ? Point.Radius : 0f;
        public bool IsContested => Point != null && Point.IsContested;

        public AITeam Owner
        {
            get
            {
                if (Point == null) return AITeam.None;
                switch (Point.Controller)
                {
                    case CapturePoint.PointController.Player: return AITeam.Player;
                    case CapturePoint.PointController.Enemy: return AITeam.Enemy;
                    default: return AITeam.None;
                }
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.8f);
            AIGizmos.Circle(transform.position, Radius);
        }
    }
}
