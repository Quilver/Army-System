using System;
using System.Collections.Generic;
using UnityEngine;

namespace LeveragedWars.Battle.AI
{
    /// <summary>A condition a trigger waits for. Conditions read the blackboard only.</summary>
    [Serializable]
    public abstract class TriggerCondition
    {
        public abstract bool Evaluate(AIBlackboard blackboard);
        public virtual void DrawGizmos() { }
    }

    // ---------------------------------------------------------------- Combinators

    [Serializable, AddTypeMenu("Logic/All Of")]
    public sealed class AllOf : TriggerCondition
    {
        [SerializeReference, SubclassSelector] public List<TriggerCondition> conditions = new List<TriggerCondition>();

        public override bool Evaluate(AIBlackboard blackboard)
        {
            bool any = false;
            foreach (TriggerCondition c in conditions)
            {
                if (c == null) continue;
                if (!c.Evaluate(blackboard)) return false;
                any = true;
            }
            return any;
        }

        public override void DrawGizmos() { foreach (TriggerCondition c in conditions) c?.DrawGizmos(); }
    }

    [Serializable, AddTypeMenu("Logic/Any Of")]
    public sealed class AnyOf : TriggerCondition
    {
        [SerializeReference, SubclassSelector] public List<TriggerCondition> conditions = new List<TriggerCondition>();

        public override bool Evaluate(AIBlackboard blackboard)
        {
            foreach (TriggerCondition c in conditions)
                if (c != null && c.Evaluate(blackboard)) return true;
            return false;
        }

        public override void DrawGizmos() { foreach (TriggerCondition c in conditions) c?.DrawGizmos(); }
    }

    [Serializable, AddTypeMenu("Logic/Not")]
    public sealed class Not : TriggerCondition
    {
        [SerializeReference, SubclassSelector] public TriggerCondition condition;

        public override bool Evaluate(AIBlackboard blackboard) => condition != null && !condition.Evaluate(blackboard);
        public override void DrawGizmos() => condition?.DrawGizmos();
    }

    // ---------------------------------------------------------------- Enemy position

    [Serializable, AddTypeMenu("Opponent/Crosses Line")]
    public sealed class OpponentCrossesLine : TriggerCondition
    {
        [Tooltip("The line runs along this marker's green (up) axis. Fires when an enemy troop is on the side its red arrow points to.")]
        public Transform line;

        [Tooltip("Half the line's length (units). 0 = endless.")]
        public float halfLength;

        public override bool Evaluate(AIBlackboard blackboard)
        {
            if (line == null) return false;
            Vector3 forward = AIGeometry.MarkerFacing(line);
            Vector3 right = AIGeometry.Right(forward);
            IReadOnlyList<IAITroop> opponents = blackboard.ActiveOpponents;
            for (int i = 0; i < opponents.Count; i++)
            {
                Vector3 v = AIGeometry.Flat(opponents[i].Position - line.position);
                if (Vector3.Dot(v, forward) <= 0f) continue;
                if (halfLength > 0f && Mathf.Abs(Vector3.Dot(v, right)) > halfLength) continue;
                return true;
            }
            return false;
        }

        public override void DrawGizmos()
        {
            if (line == null) return;
            Vector3 forward = AIGeometry.MarkerFacing(line);
            Vector3 right = AIGeometry.Right(forward);
            float length = halfLength > 0f ? halfLength : 15f;
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(line.position - right * length, line.position + right * length);
            Gizmos.DrawLine(line.position, line.position + forward * 3f);
        }
    }

    [Serializable, AddTypeMenu("Opponent/Near Point")]
    public sealed class OpponentNearPoint : TriggerCondition
    {
        public Transform point;
        public float radius = 8f;

        public override bool Evaluate(AIBlackboard blackboard) =>
            point != null && blackboard.AnyOpponentWithin(point.position, radius);

        public override void DrawGizmos()
        {
            if (point == null) return;
            Gizmos.color = Color.magenta;
            AIGizmos.Circle(point.position, radius);
        }
    }

    [Serializable, AddTypeMenu("Opponent/Near Detachment")]
    public sealed class OpponentNearDetachment : TriggerCondition
    {
        public string detachmentId;
        [Tooltip("Fires when an enemy is within this distance of any of the detachment's troops (units).")]
        public float radius = 12f;

        public override bool Evaluate(AIBlackboard blackboard)
        {
            Detachment d = blackboard.GetDetachment(detachmentId);
            if (d == null) return false;
            IReadOnlyList<AITroopAgent> agents = d.ActiveAgents;
            for (int i = 0; i < agents.Count; i++)
                if (blackboard.AnyOpponentWithin(agents[i].Position, radius)) return true;
            return false;
        }
    }

    [Serializable, AddTypeMenu("Opponent/Near Objective")]
    public sealed class OpponentNearObjective : TriggerCondition
    {
        public string objectiveId;
        public float radius = 10f;

        public override bool Evaluate(AIBlackboard blackboard)
        {
            IAIObjective o = blackboard.GetObjective(objectiveId);
            return o != null && blackboard.AnyOpponentWithin(o.Position, radius);
        }
    }

    // ---------------------------------------------------------------- Detachments and objectives

    public enum DetachmentCheck { Engaged, Broken, Done, Waiting }

    [Serializable, AddTypeMenu("Detachment Status")]
    public sealed class DetachmentStatus : TriggerCondition
    {
        public string detachmentId;
        public DetachmentCheck check = DetachmentCheck.Engaged;

        public override bool Evaluate(AIBlackboard blackboard)
        {
            Detachment d = blackboard.GetDetachment(detachmentId);
            if (d == null) return false;
            switch (check)
            {
                case DetachmentCheck.Engaged: return d.State == DetachmentState.Engaged || d.IsInCombat;
                case DetachmentCheck.Broken: return d.IsBroken;
                case DetachmentCheck.Done: return d.State == DetachmentState.Done;
                default: return d.State == DetachmentState.Waiting;
            }
        }
    }

    public enum ObjectiveOwnerCheck { AiTeam, Opponent, Nobody }

    [Serializable, AddTypeMenu("Objective/Owned")]
    public sealed class ObjectiveOwned : TriggerCondition
    {
        public string objectiveId;
        public ObjectiveOwnerCheck owner = ObjectiveOwnerCheck.AiTeam;

        public override bool Evaluate(AIBlackboard blackboard)
        {
            IAIObjective o = blackboard.GetObjective(objectiveId);
            if (o == null) return false;
            switch (owner)
            {
                case ObjectiveOwnerCheck.AiTeam: return o.Owner == blackboard.AiTeam;
                case ObjectiveOwnerCheck.Opponent: return o.Owner != blackboard.AiTeam && o.Owner != AITeam.None;
                default: return o.Owner == AITeam.None;
            }
        }
    }

    [Serializable, AddTypeMenu("Objective/Contested")]
    public sealed class ObjectiveContested : TriggerCondition
    {
        public string objectiveId;

        public override bool Evaluate(AIBlackboard blackboard)
        {
            IAIObjective o = blackboard.GetObjective(objectiveId);
            return o != null && o.IsContested;
        }
    }

    // ---------------------------------------------------------------- Time and flags

    [Serializable, AddTypeMenu("Time Elapsed")]
    public sealed class TimeElapsed : TriggerCondition
    {
        public float seconds = 30f;

        [Tooltip("Count from when this trigger fired. Leave empty to count from the start of the battle.")]
        public string sinceTriggerId;

        public override bool Evaluate(AIBlackboard blackboard)
        {
            if (string.IsNullOrEmpty(sinceTriggerId)) return blackboard.Elapsed >= seconds;
            return blackboard.TryGetTriggerFireTime(sinceTriggerId, out float fired) && blackboard.Time - fired >= seconds;
        }
    }

    [Serializable, AddTypeMenu("Flag Is Set")]
    public sealed class FlagIsSet : TriggerCondition
    {
        public string flag;
        public override bool Evaluate(AIBlackboard blackboard) => blackboard.HasFlag(flag);
    }
}
