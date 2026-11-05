using System;
using System.Collections.Generic;
using UnityEngine;

namespace LeveragedWars.Battle.AI
{
    /// <summary>Serialisable settings for a formation. Each subclass creates its runtime formation.</summary>
    [Serializable]
    public abstract class FormationDefinition
    {
        public abstract Formation Create();
    }

    /// <summary>
    /// Detachment layer: a role that picks targets, routes and positions, and orders only the
    /// troops in its own detachment.
    /// </summary>
    public abstract class Formation
    {
        protected const float ArrivalRadius = 1.5f;

        protected Detachment Detachment { get; private set; }
        protected AIBlackboard Blackboard { get; private set; }
        protected IReadOnlyList<AITroopAgent> Troops => Detachment.ActiveAgents;

        public DetachmentState State { get; protected set; } = DetachmentState.Waiting;
        public virtual string Name => GetType().Name.Replace("Formation", string.Empty);

        internal void Enter(Detachment detachment, AIBlackboard blackboard)
        {
            Detachment = detachment;
            Blackboard = blackboard;
            OnEnter();
        }

        protected virtual void OnEnter() { }
        public abstract void Tick();
        public virtual void Exit() { }
        public virtual void DrawGizmos() { }
    }

    public enum AnchorKind { StartPosition, Transform, Detachment, Objective }

    /// <summary>A point a formation is tied to: where it started, a scene marker, another detachment or an objective.</summary>
    [Serializable]
    public sealed class AnchorReference
    {
        public AnchorKind kind = AnchorKind.StartPosition;

        [Tooltip("Kind = Transform. Its right (red arrow) sets the facing.")]
        public Transform transform;

        [Tooltip("Kind = Detachment.")]
        public string detachmentId;

        [Tooltip("Kind = Objective. Faces the nearest enemy.")]
        public string objectiveId;

        [Tooltip("Units to the anchor's right (negative = left).")]
        public float sideOffset;

        [Tooltip("Units along the anchor's facing (negative = behind).")]
        public float forwardOffset;

        /// <summary>Resolves the anchor. Falls back to the detachment's start when the target is missing.</summary>
        public bool TryResolve(Detachment self, AIBlackboard blackboard, out Vector3 position, out Vector3 facing)
        {
            position = self.StartPosition;
            facing = self.StartFacing;
            bool ok;
            switch (kind)
            {
                case AnchorKind.StartPosition:
                    ok = true;
                    break;
                case AnchorKind.Transform:
                    ok = transform != null;
                    if (ok)
                    {
                        position = transform.position;
                        facing = AIGeometry.MarkerFacing(transform);
                    }
                    break;
                case AnchorKind.Detachment:
                    Detachment other = blackboard.GetDetachment(detachmentId);
                    ok = other != null && other.ActiveAgents.Count > 0;
                    if (ok)
                    {
                        position = other.Centre;
                        facing = other.Facing;
                    }
                    break;
                case AnchorKind.Objective:
                    IAIObjective objective = blackboard.GetObjective(objectiveId);
                    ok = objective != null;
                    if (ok)
                    {
                        position = objective.Position;
                        IAITroop threat = blackboard.NearestOpponent(objective.Position);
                        if (threat != null) facing = AIGeometry.Direction(objective.Position, threat.Position, facing);
                    }
                    break;
                default:
                    ok = false;
                    break;
            }
            position += AIGeometry.Right(facing) * sideOffset + facing * forwardOffset;
            return ok;
        }
    }

    public struct LineSlot
    {
        public AITroopAgent Agent;
        public Vector3 Position;
    }

    public static class FormationUtility
    {
        static readonly List<AITroopAgent> sorted = new List<AITroopAgent>();

        /// <summary>
        /// Places troops side by side in a line, keeping their current left-to-right order so
        /// their paths don't cross.
        /// </summary>
        public static void ComputeLine(IReadOnlyList<AITroopAgent> agents, Vector3 centre, Vector3 facing, float gap, List<LineSlot> slots)
        {
            slots.Clear();
            sorted.Clear();
            for (int i = 0; i < agents.Count; i++) sorted.Add(agents[i]);

            Vector3 right = AIGeometry.Right(facing);
            sorted.Sort((a, b) => Vector3.Dot(a.Position - centre, right).CompareTo(Vector3.Dot(b.Position - centre, right)));

            float total = gap * Mathf.Max(0, sorted.Count - 1);
            for (int i = 0; i < sorted.Count; i++) total += Width(sorted[i]);

            float x = -total * 0.5f;
            for (int i = 0; i < sorted.Count; i++)
            {
                float w = Width(sorted[i]);
                slots.Add(new LineSlot { Agent = sorted[i], Position = centre + right * (x + w * 0.5f) });
                x += w + gap;
            }
        }

        static float Width(AITroopAgent agent) => Mathf.Max(0.5f, agent.Troop.Frontage);

        /// <summary>Moves to the slot, then holds there. Returns true while still moving.</summary>
        public static bool MoveOrHold(AITroopAgent agent, Vector3 slot, Vector3 facing, bool run, float arrivalRadius)
        {
            if (AIGeometry.FlatDistance(agent.Position, slot) > arrivalRadius)
            {
                agent.MoveTo(slot, facing, run);
                return true;
            }
            agent.Hold(slot, facing);
            return false;
        }

        public static IAITroop Nearest(List<IAITroop> troops, Vector3 from)
        {
            IAITroop best = null;
            float bestSq = float.PositiveInfinity;
            for (int i = 0; i < troops.Count; i++)
            {
                float sq = AIGeometry.Flat(troops[i].Position - from).sqrMagnitude;
                if (sq < bestSq) { bestSq = sq; best = troops[i]; }
            }
            return best;
        }

        public static bool IsValidTarget(IAITroop troop) => troop != null && troop.IsAlive && !troop.IsRouting;
    }

    /// <summary>
    /// Base for formations that hold near an anchor, engage threats with their melee troops and
    /// return to the anchor once past the leash. Ranged troops hold their slot and shoot by reflex.
    /// </summary>
    public abstract class AnchoredFormation : Formation
    {
        readonly List<LineSlot> slots = new List<LineSlot>();
        readonly HashSet<AITroopAgent> returning = new HashSet<AITroopAgent>();
        protected readonly List<IAITroop> Threats = new List<IAITroop>();

        protected Vector3 AnchorPosition { get; private set; }
        protected Vector3 AnchorFacing { get; private set; }

        protected abstract float LeashRange { get; }
        protected abstract float LineGap { get; }
        protected abstract void ResolveAnchor(out Vector3 position, out Vector3 facing);
        protected abstract void FindThreats(List<IAITroop> results);

        public override void Tick()
        {
            ResolveAnchor(out Vector3 anchor, out Vector3 facing);
            AnchorPosition = anchor;
            AnchorFacing = facing;
            FindThreats(Threats);
            FormationUtility.ComputeLine(Troops, anchor, facing, LineGap, slots);

            bool engaged = false, moving = false, back = false;
            foreach (LineSlot slot in slots)
            {
                AITroopAgent a = slot.Agent;
                float fromAnchor = AIGeometry.FlatDistance(a.Position, anchor);
                if (fromAnchor > LeashRange)
                    returning.Add(a);
                else if (returning.Contains(a) &&
                         (fromAnchor < LeashRange * 0.5f || AIGeometry.FlatDistance(a.Position, slot.Position) <= ArrivalRadius * 2f))
                    returning.Remove(a);

                if (returning.Contains(a))
                {
                    FormationUtility.MoveOrHold(a, slot.Position, facing, true, ArrivalRadius);
                    back = true;
                    continue;
                }

                if (Threats.Count > 0 && !a.Troop.IsRanged)
                {
                    a.Attack(FormationUtility.Nearest(Threats, a.Position));
                    engaged = true;
                    continue;
                }

                if (FormationUtility.MoveOrHold(a, slot.Position, facing, false, ArrivalRadius)) moving = true;
                if (a.Troop.IsInMelee || (a.Troop.IsRanged && a.Issued.Kind == AIOrderKind.Attack)) engaged = true;
            }

            State = engaged ? DetachmentState.Engaged
                : back ? DetachmentState.Returning
                : moving ? DetachmentState.Moving
                : DetachmentState.Waiting;
        }

        public override void Exit() => returning.Clear();

        public override void DrawGizmos()
        {
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.7f);
            Gizmos.DrawLine(AnchorPosition, AnchorPosition + AnchorFacing * 3f);
            AIGizmos.Circle(AnchorPosition, LeashRange);
        }
    }
}
