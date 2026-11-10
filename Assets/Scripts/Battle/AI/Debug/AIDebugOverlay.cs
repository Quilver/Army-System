using System.Collections.Generic;
using UnityEngine;

namespace LeveragedWars.Battle.AI
{
    /// <summary>
    /// Draws each detachment's formation, state and troop orders. Visible in the Scene view,
    /// and in the Game view with Gizmos switched on.
    /// </summary>
    public sealed class AIDebugOverlay : MonoBehaviour
    {
        [SerializeField] AIDirector director;
        [SerializeField] bool labels = true;
        [SerializeField] bool orders = true;
        [SerializeField] bool formationShapes = true;
        [SerializeField, Tooltip("How far above a detachment's centre its label is drawn (units).")] float labelHeight = 4f;

        void OnDrawGizmos()
        {
            if (director == null || !director.IsRunning) return;
            IReadOnlyList<Detachment> detachments = director.Blackboard.Detachments;
            for (int i = 0; i < detachments.Count; i++)
            {
                Detachment d = detachments[i];
                Gizmos.color = StateColour(d.State);
                Gizmos.DrawWireSphere(d.Centre, 0.6f);
                if (formationShapes) d.Formation?.DrawGizmos();
                if (orders) foreach (AITroopAgent a in d.ActiveAgents) DrawOrder(a);
#if UNITY_EDITOR
                if (labels)
                {
                    string formation = d.Formation != null ? d.Formation.Name : "No formation";
                    UnityEditor.Handles.Label(d.Centre + Vector3.up * labelHeight,
                        $"{d.Id}\n{formation} · {d.State}\n{Mathf.RoundToInt(d.Cohesion * 100f)}% fighting");
                }
#endif
            }
        }

        void DrawOrder(AITroopAgent agent)
        {
            Vector3 from = agent.Position;
            AIOrder order = agent.Issued;
            switch (order.Kind)
            {
                case AIOrderKind.Attack:
                    Gizmos.color = Color.red;
                    if (order.Target != null) Gizmos.DrawLine(from, order.Target.Position);
                    break;
                case AIOrderKind.Charge:
                    Gizmos.color = Color.magenta;
                    if (order.Target != null) Gizmos.DrawLine(from, order.Target.Position);
                    break;
                case AIOrderKind.Move:
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(from, order.Position);
                    break;
                case AIOrderKind.FallBack:
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(from, order.Position);
                    break;
                case AIOrderKind.Hold:
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(from, from + order.Facing * 2f);
                    break;
            }
#if UNITY_EDITOR
            if (labels && agent.Reflex != null) UnityEditor.Handles.Label(from + Vector3.up * (labelHeight * 0.5f), agent.Reflex);
#endif
        }

        static Color StateColour(DetachmentState state) => state switch
        {
            DetachmentState.Engaged => Color.red,
            DetachmentState.Moving => Color.green,
            DetachmentState.Returning => Color.yellow,
            DetachmentState.Done => Color.grey,
            DetachmentState.Broken => Color.black,
            _ => Color.cyan
        };
    }
}
