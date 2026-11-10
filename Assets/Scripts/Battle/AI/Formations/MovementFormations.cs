using System;
using System.Collections.Generic;
using UnityEngine;

namespace LeveragedWars.Battle.AI
{
    // ---------------------------------------------------------------- Advance

    public enum AdvanceTarget { NearestOpponent, Point }

    [Serializable, AddTypeMenu("Advance")]
    public sealed class AdvanceDefinition : FormationDefinition
    {
        public AdvanceTarget target = AdvanceTarget.NearestOpponent;

        [Tooltip("Target = Point.")]
        public AnchorReference point = new AnchorReference { kind = AnchorKind.Transform };

        [Tooltip("Melee troops attack enemies within this distance (units).")]
        public float engageRange = 12f;

        [Tooltip("Melee troops charge rather than walk in from this distance (units).")]
        public float chargeDistance = 8f;

        [Tooltip("Advance as a line. Untick to let each troop head straight for its nearest enemy.")]
        public bool keepLine = true;

        [Tooltip("Gap between troops in the line (units).")]
        public float lineGap = 2f;

        public override Formation Create() => new AdvanceFormation(this);
    }

    /// <summary>Move as a line towards the nearest enemy (or a point), then engage.</summary>
    public sealed class AdvanceFormation : Formation
    {
        readonly AdvanceDefinition def;
        readonly List<LineSlot> slots = new List<LineSlot>();
        Vector3 destination;

        public AdvanceFormation(AdvanceDefinition definition) => def = definition;

        public override void Tick()
        {
            Vector3 centre = Detachment.Centre;
            Vector3 facing;
            bool arrived;
            IAITroop focus = Blackboard.NearestOpponent(centre);

            if (def.target == AdvanceTarget.Point)
            {
                def.point.TryResolve(Detachment, Blackboard, out destination, out Vector3 pointFacing);
                arrived = AIGeometry.FlatDistance(centre, destination) <= ArrivalRadius * 2f;
                facing = arrived ? pointFacing : AIGeometry.Direction(centre, destination, pointFacing);
            }
            else if (focus != null)
            {
                facing = AIGeometry.Direction(centre, focus.Position, Detachment.Facing);
                // Line up short of the enemy; troops engage as they come within range.
                destination = focus.Position - facing * (def.engageRange * 0.5f);
                arrived = false;
            }
            else
            {
                destination = centre;
                facing = Detachment.Facing;
                arrived = true;
            }

            FormationUtility.ComputeLine(Troops, destination, facing, def.lineGap, slots);
            bool engaged = false;
            foreach (LineSlot slot in slots)
            {
                AITroopAgent a = slot.Agent;
                if (TryEngage(a))
                {
                    engaged = true;
                    continue;
                }
                if (def.keepLine || focus == null)
                    FormationUtility.MoveOrHold(a, slot.Position, facing, false, ArrivalRadius);
                else
                    a.Attack(Blackboard.NearestOpponent(a.Position));
                if (a.Troop.IsInMelee) engaged = true;
            }

            State = engaged ? DetachmentState.Engaged : arrived ? DetachmentState.Done : DetachmentState.Moving;
        }

        bool TryEngage(AITroopAgent agent)
        {
            IAITroop troop = agent.Troop;
            float reach = troop.IsRanged ? troop.MaxRange : def.engageRange;
            IAITroop near = Blackboard.NearestOpponent(agent.Position, reach);
            if (near == null) return false;

            if (troop.IsRanged)
            {
                if (!troop.HasAmmo) return false;
                // Stop and shoot; the troop's reflex picks the target.
                agent.Hold(agent.Position, AIGeometry.Direction(agent.Position, near.Position, troop.Forward));
                return true;
            }

            float distance = AIGeometry.FlatDistance(agent.Position, near.Position);
            if (distance <= def.chargeDistance && !near.IsInMelee && !troop.IsInMelee) agent.Charge(near);
            else agent.Attack(near);
            return true;
        }

        public override void DrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.8f);
            Gizmos.DrawLine(Detachment.Centre, destination);
            Gizmos.DrawWireSphere(destination, 0.75f);
        }
    }

    // ---------------------------------------------------------------- Flank

    public enum FlankTarget { RearmostOpponent, NearestOpponentToDetachment, OpponentEngagedWithDetachment }

    [Serializable, AddTypeMenu("Flank")]
    public sealed class FlankDefinition : FormationDefinition
    {
        public FlankTarget target = FlankTarget.RearmostOpponent;

        [Tooltip("Detachment id for the two 'Detachment' target modes.")]
        public string detachmentId;

        [Tooltip("Authored waypoints followed in order before the approach, e.g. round a river. Leave empty to go direct.")]
        public List<Transform> route = new List<Transform>();

        [Tooltip("A waypoint counts as reached within this distance (units).")]
        public float waypointRadius = 3f;

        [Tooltip("Skip a waypoint after getting no closer to it for this long (seconds), e.g. one placed off the walkable ground.")]
        public float waypointStuckSeconds = 4f;

        [Tooltip("How far to the side or rear of the target to aim for (units).")]
        public float flankDistance = 8f;

        [Tooltip("Charge once within this distance and beside or behind the target (units).")]
        public float chargeDistance = 12f;

        [Tooltip("Charge anyway after approaching for this long (seconds).")]
        public float approachTimeout = 20f;

        public override Formation Create() => new FlankFormation(this);
    }

    /// <summary>Follow a route to the side or rear of a target, then charge.</summary>
    public sealed class FlankFormation : Formation
    {
        enum Phase { Route, Approach, Engage }

        readonly FlankDefinition def;
        readonly List<LineSlot> slots = new List<LineSlot>();
        Phase phase;
        int waypoint;
        float approachStarted;
        IAITroop target;
        Vector3 aimPoint;
        float closestToWaypoint;
        float lastProgress;

        public FlankFormation(FlankDefinition definition) => def = definition;

        public override string Name => $"Flank ({phase})";

        protected override void OnEnter()
        {
            waypoint = 0;
            phase = def.route != null && def.route.Count > 0 ? Phase.Route : Phase.Approach;
            approachStarted = Blackboard.Time;
            ResetProgress();
        }

        public override void Tick()
        {
            switch (phase)
            {
                case Phase.Route: TickRoute(); break;
                case Phase.Approach: TickApproach(); break;
                default: TickEngage(); break;
            }
        }

        void TickRoute()
        {
            while (waypoint < def.route.Count && def.route[waypoint] == null) waypoint++;
            if (waypoint >= def.route.Count)
            {
                phase = Phase.Approach;
                approachStarted = Blackboard.Time;
                TickApproach();
                return;
            }

            aimPoint = def.route[waypoint].position;
            float distance = AIGeometry.FlatDistance(Detachment.Centre, aimPoint);
            if (distance < closestToWaypoint - 0.5f)
            {
                closestToWaypoint = distance;
                lastProgress = Blackboard.Time;
            }
            bool stuck = Blackboard.Time - lastProgress >= def.waypointStuckSeconds;
            if (distance <= def.waypointRadius || stuck)
            {
                if (stuck) Debug.LogWarning($"[AI] {Detachment.Id} could not reach waypoint '{def.route[waypoint].name}'; skipping it.");
                waypoint++;
                ResetProgress();
                TickRoute();
                return;
            }

            Vector3 facing = AIGeometry.Direction(Detachment.Centre, aimPoint, Detachment.Facing);
            FormationUtility.ComputeLine(Troops, aimPoint, facing, 1f, slots);
            foreach (LineSlot slot in slots) slot.Agent.MoveTo(slot.Position, facing, true);
            State = DetachmentState.Moving;
        }

        void ResetProgress()
        {
            closestToWaypoint = float.PositiveInfinity;
            lastProgress = Blackboard.Time;
        }

        void TickApproach()
        {
            if (!FormationUtility.IsValidTarget(target)) target = PickTarget();
            if (target == null)
            {
                HoldHere();
                return;
            }

            bool timedOut = Blackboard.Time - approachStarted >= def.approachTimeout;
            Vector3 targetForward = AIGeometry.FlatForward(target.Forward);
            Vector3 targetRight = AIGeometry.Right(targetForward);
            bool anyCharging = false;

            foreach (AITroopAgent a in Troops)
            {
                Vector3 fromTarget = AIGeometry.Direction(target.Position, a.Position, -targetForward);
                bool besideOrBehind = Vector3.Dot(fromTarget, targetForward) < 0.3f;
                float distance = AIGeometry.FlatDistance(a.Position, target.Position);

                if (distance <= def.chargeDistance && (besideOrBehind || timedOut))
                {
                    a.Charge(target);
                    anyCharging = true;
                    continue;
                }

                // In front: swing out to the nearer side first. Beside or behind: aim for the rear.
                if (besideOrBehind)
                {
                    aimPoint = target.Position - targetForward * def.flankDistance;
                }
                else
                {
                    float side = Mathf.Sign(Vector3.Dot(fromTarget, targetRight));
                    aimPoint = target.Position + targetRight * (side * def.flankDistance * 1.5f);
                }
                a.MoveTo(aimPoint, AIGeometry.Direction(aimPoint, target.Position, targetForward), true);
            }

            if (anyCharging) phase = Phase.Engage;
            State = anyCharging ? DetachmentState.Engaged : DetachmentState.Moving;
        }

        void TickEngage()
        {
            if (!FormationUtility.IsValidTarget(target)) target = Blackboard.NearestOpponent(Detachment.Centre);
            if (target == null)
            {
                HoldHere();
                return;
            }

            foreach (AITroopAgent a in Troops)
            {
                if (a.Troop.IsInMelee) a.Attack(target);
                else if (!(a.Requested.Kind == AIOrderKind.Charge && ReferenceEquals(a.Requested.Target, target))) a.Charge(target);
            }
            State = DetachmentState.Engaged;
        }

        IAITroop PickTarget()
        {
            Detachment reference = Blackboard.GetDetachment(def.detachmentId);
            Vector3 referenceCentre = reference != null ? reference.Centre : Detachment.Centre;
            switch (def.target)
            {
                case FlankTarget.OpponentEngagedWithDetachment:
                    return Blackboard.OpponentEngagedWith(reference) ?? Blackboard.NearestOpponent(referenceCentre);
                case FlankTarget.NearestOpponentToDetachment:
                    return Blackboard.NearestOpponent(referenceCentre);
                default:
                    return Blackboard.RearmostOpponent();
            }
        }

        void HoldHere()
        {
            foreach (AITroopAgent a in Troops) a.Hold(a.Position, a.Troop.Forward);
            State = DetachmentState.Waiting;
        }

        public override void DrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.9f);
            Vector3 previous = Detachment.Centre;
            if (phase == Phase.Route)
            {
                for (int i = waypoint; i < def.route.Count; i++)
                {
                    if (def.route[i] == null) continue;
                    Gizmos.DrawLine(previous, def.route[i].position);
                    previous = def.route[i].position;
                }
            }
            else
            {
                Gizmos.DrawWireSphere(aimPoint, 0.75f);
            }
            if (target != null) Gizmos.DrawLine(Detachment.Centre, target.Position);
        }
    }

    // ---------------------------------------------------------------- Capture

    [Serializable, AddTypeMenu("Capture")]
    public sealed class CaptureDefinition : FormationDefinition
    {
        [Tooltip("Must match the objective's IAIObjective.Id.")]
        public string objectiveId;

        [Tooltip("Stop to fight enemies within this distance (units), then carry on.")]
        public float aggroRange = 6f;

        public bool run;

        public override Formation Create() => new CaptureFormation(this);
    }

    /// <summary>Move to an objective and take it; fight only enemies within aggro range. Holds it once captured.</summary>
    public sealed class CaptureFormation : Formation
    {
        readonly CaptureDefinition def;
        readonly List<IAITroop> threats = new List<IAITroop>();
        readonly List<LineSlot> slots = new List<LineSlot>();

        public CaptureFormation(CaptureDefinition definition) => def = definition;

        public override void Tick()
        {
            Blackboard.OpponentsWithin(Detachment.Centre, def.aggroRange, threats);
            if (threats.Count > 0)
            {
                foreach (AITroopAgent a in Troops)
                {
                    IAITroop nearest = FormationUtility.Nearest(threats, a.Position);
                    if (a.Troop.IsRanged) a.Hold(a.Position, AIGeometry.Direction(a.Position, nearest.Position, a.Troop.Forward));
                    else a.Attack(nearest);
                }
                State = DetachmentState.Engaged;
                return;
            }

            IAIObjective objective = Blackboard.GetObjective(def.objectiveId);
            if (objective == null)
            {
                foreach (AITroopAgent a in Troops) a.Hold(a.Position, a.Troop.Forward);
                State = DetachmentState.Waiting;
                return;
            }

            IAITroop enemy = Blackboard.NearestOpponent(objective.Position);
            Vector3 facing = enemy != null
                ? AIGeometry.Direction(objective.Position, enemy.Position, Detachment.Facing)
                : Detachment.Facing;
            FormationUtility.ComputeLine(Troops, objective.Position, facing, 1f, slots);
            foreach (LineSlot slot in slots)
                FormationUtility.MoveOrHold(slot.Agent, slot.Position, facing, def.run, ArrivalRadius);

            bool captured = objective.Owner == Detachment.Team && !objective.IsContested;
            State = captured ? DetachmentState.Done : DetachmentState.Moving;
        }
    }

    // ---------------------------------------------------------------- Fall back

    [Serializable, AddTypeMenu("Fall Back")]
    public sealed class FallBackDefinition : FormationDefinition
    {
        public AnchorReference point = new AnchorReference { kind = AnchorKind.Transform };

        [Tooltip("Gap between troops in the line (units).")]
        public float lineGap = 2f;

        public override Formation Create() => new FallBackFormation(this);
    }

    /// <summary>Withdraw to a point without routing, then hold there.</summary>
    public sealed class FallBackFormation : Formation
    {
        readonly FallBackDefinition def;
        readonly List<LineSlot> slots = new List<LineSlot>();

        public FallBackFormation(FallBackDefinition definition) => def = definition;

        public override void Tick()
        {
            def.point.TryResolve(Detachment, Blackboard, out Vector3 position, out Vector3 facing);
            FormationUtility.ComputeLine(Troops, position, facing, def.lineGap, slots);
            bool moving = false;
            foreach (LineSlot slot in slots)
            {
                if (AIGeometry.FlatDistance(slot.Agent.Position, slot.Position) > ArrivalRadius)
                {
                    slot.Agent.FallBack(slot.Position);
                    moving = true;
                }
                else
                {
                    slot.Agent.Hold(slot.Position, facing);
                }
            }
            State = moving ? DetachmentState.Moving : DetachmentState.Done;
        }
    }
}
