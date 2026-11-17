using System;
using System.Collections.Generic;
using UnityEngine;

namespace LeveragedWars.Battle.AI
{
    // ---------------------------------------------------------------- Chain Line

    public enum ChainTarget { OpponentCentre, NearestOpponent, Point }

    [Serializable, AddTypeMenu("Chain Line")]
    public sealed class ChainLineDefinition : FormationDefinition
    {
        [Tooltip("What the chain faces and advances on: the centre of the opposing army, the nearest opponent, or a point.")]
        public ChainTarget target = ChainTarget.OpponentCentre;

        [Tooltip("Target = Point. Its red arrow is not used: the chain faces the way it advances.")]
        public AnchorReference point = new AnchorReference { kind = AnchorKind.Transform };

        [Tooltip("Advance on the target. Untick to hold the chain where it forms; it still fights what comes into reach.")]
        public bool advance = true;

        [Tooltip("How far one bound takes the chain (units). The next bound waits until every troop stands in its place, linked.")]
        public float boundDistance = 6f;

        [Tooltip("A troop counts as in its place within this distance of it (units).")]
        public float placeTolerance = 2f;

        [Tooltip("Go on without a troop that has not reached its place, linked, after this long (seconds).")]
        public float waitTimeout = 8f;

        [Tooltip("The chain stops advancing this far short of its target (units). Fighting usually starts before.")]
        public float stopDistance = 4f;

        [Tooltip("Turn the chain to face its target when it is more than this far off (degrees), at the next bound.")]
        public float refaceThreshold = 10f;

        [Tooltip("The most the chain turns in one bound (degrees).")]
        public float maxTurnPerBound = 25f;

        [Tooltip("A troop attacks an opponent in front of it within this distance (units, pivot to pivot).")]
        public float attackRange = 8f;

        [Tooltip("...and no further to the side of the troop's own front than this, beyond half its width (units), so the " +
                 "chain does not bend to reach it.")]
        public float attackSideReach = 4f;

        [Tooltip("Re-form and go on once nobody in the chain has had an opponent in reach for this long (seconds).")]
        public float regroupSeconds = 2f;

        public override Formation Create() => new ChainLineFormation(this);
    }

    /// <summary>
    /// A chain of linked battle lines (spec §3.4) that advances and fights as one. The troops stand end to end, left to
    /// right in the order they started in, each joined to the next by the same order link as a multi-troop line order
    /// (IAIChainTroop.LinkRightTo). The chain advances in bounds: each bound moves the whole line forward, and the next
    /// waits until every troop stands in its place and every link is in force (or Wait Timeout).
    /// Fighting: a troop with an opponent in reach in front of it attacks it (closes in; its Assault opens on contact).
    /// Once in melee with an opponent in front it stands its ground there under a line order, so it keeps its links and
    /// can link again; an opponent on its flank or rear it turns on (attacks). The others move up level with the fight
    /// and hold their places. Once nobody has had an opponent in reach for Regroup Seconds the chain re-forms where it is
    /// and goes on. The troop system keeps, stretches and breaks the links by its own rules.
    /// </summary>
    public sealed class ChainLineFormation : Formation
    {
        public enum ChainPhase { Forming, Advancing, Fighting }

        readonly ChainLineDefinition def;
        readonly List<AITroopAgent> order = new List<AITroopAgent>();
        readonly List<AITroopAgent> active = new List<AITroopAgent>();
        readonly List<Vector3> slots = new List<Vector3>();
        readonly Dictionary<AITroopAgent, IAITroop> targets = new Dictionary<AITroopAgent, IAITroop>();
        // Where each troop in melee with an opponent in front stands its ground (fixed when it got there).
        readonly Dictionary<AITroopAgent, Vector3> holdSpots = new Dictionary<AITroopAgent, Vector3>();
        bool planned;
        float boundStarted;
        float lastContact = float.NegativeInfinity;

        public ChainLineFormation(ChainLineDefinition definition) => def = definition;

        public ChainPhase Phase { get; private set; } = ChainPhase.Forming;
        /// <summary>Middle of the chain's line of pivots, where the troops are told to stand.</summary>
        public Vector3 LineCentre { get; private set; }
        public Vector3 LineFacing { get; private set; } = Vector3.right;
        /// <summary>The troops in chain order, left to right (as seen facing the enemy).</summary>
        public IReadOnlyList<AITroopAgent> ChainOrder => active;
        /// <summary>Where each troop of ChainOrder is told to stand (its pivot).</summary>
        public IReadOnlyList<Vector3> Slots => slots;

        public override string Name => $"Chain Line ({Phase})";

        protected override void OnEnter()
        {
            planned = false;
            holdSpots.Clear();
            Phase = ChainPhase.Forming;
            lastContact = float.NegativeInfinity;
        }

        public override void Tick()
        {
            RefreshActive();
            if (active.Count == 0)
            {
                State = DetachmentState.Waiting;
                return;
            }

            bool hasTarget = TryResolveTarget(out Vector3 targetPosition);
            if (!planned) Plan(hasTarget, targetPosition);

            // ---- Contact: every troop with an opponent in reach attacks it.
            bool contact = FindTargets();
            if (contact) lastContact = Blackboard.Time;
            if (contact || (Phase == ChainPhase.Fighting && Blackboard.Time - lastContact < def.regroupSeconds))
            {
                Fight();
                return;
            }
            if (Phase == ChainPhase.Fighting) Regroup(hasTarget, targetPosition);

            // ---- Forming or advancing: hold the places; bound on once the chain stands formed.
            ComputeSlots();
            bool ready = IsFormed();
            bool waitedTooLong = Blackboard.Time - boundStarted >= def.waitTimeout;
            if ((ready || waitedTooLong) && def.advance && hasTarget && NextBound(targetPosition))
                ComputeSlots();

            bool moving = false;
            for (int i = 0; i < active.Count; i++)
            {
                active[i].HoldInLine(slots[i], LineFacing);
                if (AIGeometry.FlatDistance(active[i].Position, slots[i]) > def.placeTolerance) moving = true;
            }
            LinkNeighbours();
            State = moving ? DetachmentState.Moving : DetachmentState.Waiting;
        }

        // ---- Planning ----

        void Plan(bool hasTarget, Vector3 targetPosition)
        {
            planned = true;
            LineCentre = AIGeometry.Flat(Detachment.Centre);
            LineFacing = hasTarget ? AIGeometry.Direction(LineCentre, targetPosition, Detachment.Facing) : Detachment.Facing;
            SortLeftToRight();
            Phase = ChainPhase.Forming;
            boundStarted = Blackboard.Time;
        }

        // Keeps the chain order; troops that drop out are skipped, troops that come back go where they stand.
        void RefreshActive()
        {
            IReadOnlyList<AITroopAgent> agents = Detachment.Agents;
            for (int i = 0; i < agents.Count; i++)
                if (!order.Contains(agents[i])) Insert(agents[i]);
            for (int i = order.Count - 1; i >= 0; i--)
                if (order[i].Detachment != Detachment) order.RemoveAt(i);

            active.Clear();
            for (int i = 0; i < order.Count; i++)
                if (order[i].IsActive) active.Add(order[i]);
        }

        void Insert(AITroopAgent agent)
        {
            Vector3 right = AIGeometry.Right(LineFacing);
            float x = Vector3.Dot(AIGeometry.Flat(agent.Position - LineCentre), right);
            int index = 0;
            while (index < order.Count && Vector3.Dot(AIGeometry.Flat(order[index].Position - LineCentre), right) <= x) index++;
            order.Insert(index, agent);
        }

        void SortLeftToRight()
        {
            Vector3 right = AIGeometry.Right(LineFacing);
            Vector3 centre = LineCentre;
            order.Sort((a, b) => Vector3.Dot(a.Position - centre, right).CompareTo(Vector3.Dot(b.Position - centre, right)));
            active.Clear();
            for (int i = 0; i < order.Count; i++)
                if (order[i].IsActive) active.Add(order[i]);
        }

        bool TryResolveTarget(out Vector3 position)
        {
            position = default;
            switch (def.target)
            {
                case ChainTarget.Point:
                    return def.point.TryResolve(Detachment, Blackboard, out position, out _);
                case ChainTarget.NearestOpponent:
                    IAITroop nearest = Blackboard.NearestOpponent(Detachment.Centre);
                    if (nearest == null) return false;
                    position = nearest.Position;
                    return true;
                default:
                    IReadOnlyList<IAITroop> opponents = Blackboard.ActiveOpponents;
                    if (opponents.Count == 0) return false;
                    Vector3 sum = Vector3.zero;
                    for (int i = 0; i < opponents.Count; i++) sum += AIGeometry.Flat(opponents[i].Position);
                    position = sum / opponents.Count;
                    return true;
            }
        }

        /// <summary>The troops' places: end to end along the line, each its natural line length wide.</summary>
        void ComputeSlots()
        {
            slots.Clear();
            Vector3 right = AIGeometry.Right(LineFacing);
            float total = 0f;
            for (int i = 0; i < active.Count; i++) total += Width(active[i]);
            float x = -total * 0.5f;
            for (int i = 0; i < active.Count; i++)
            {
                float w = Width(active[i]);
                slots.Add(LineCentre + right * (x + w * 0.5f));
                x += w;
            }
        }

        static float Width(AITroopAgent agent)
        {
            float natural = agent.Troop is IAIChainTroop chain ? chain.NaturalLineLength : 0f;
            return Mathf.Max(0.5f, natural > 0f ? natural : agent.Troop.Frontage);
        }

        /// <summary>Every troop in its place and every pair of neighbours linked.</summary>
        bool IsFormed()
        {
            for (int i = 0; i < active.Count; i++)
                if (AIGeometry.FlatDistance(active[i].Position, slots[i]) > def.placeTolerance) return false;
            for (int i = 0; i + 1 < active.Count; i++)
            {
                if (!(active[i].Troop is IAIChainTroop left) || !(active[i + 1].Troop is IAIChainTroop right)) continue;
                if (left.RightLinkTo(right) != AIChainLink.Linked) return false;
            }
            return true;
        }

        /// <summary>Moves the line one bound towards the target, turning towards it first. False if it stays.</summary>
        bool NextBound(Vector3 targetPosition)
        {
            Vector3 desired = AIGeometry.Direction(LineCentre, targetPosition, LineFacing);
            float off = Vector3.SignedAngle(LineFacing, desired, Vector3.forward);
            bool turn = Mathf.Abs(off) > def.refaceThreshold;
            Vector3 facing = turn ? AIGeometry.Rotate(LineFacing, Mathf.Clamp(off, -def.maxTurnPerBound, def.maxTurnPerBound)) : LineFacing;

            float ahead = Vector3.Dot(AIGeometry.Flat(targetPosition - LineCentre), facing) - StopDistance;
            float step = Mathf.Clamp(ahead, 0f, def.boundDistance);
            if (!turn && step < 0.5f) return false;

            LineFacing = facing;
            LineCentre += facing * step;
            boundStarted = Blackboard.Time;
            Phase = ChainPhase.Advancing;
            return true;
        }

        float StopDistance => def.target == ChainTarget.Point ? 0f : def.stopDistance;

        /// <summary>Joins each pair of neighbours that is not linked yet, once both have their line orders.</summary>
        void LinkNeighbours()
        {
            for (int i = 0; i + 1 < active.Count; i++)
            {
                if (!(active[i].Troop is IAIChainTroop left) || !(active[i + 1].Troop is IAIChainTroop right)) continue;
                if (left.RightLinkTo(right) != AIChainLink.None) continue;
                if (!left.HasLineOrder || !right.HasLineOrder) continue; // the agents issue their orders on their own tick
                left.LinkRightTo(right);
            }
        }

        // ---- Fighting ----

        /// <summary>Picks each troop's opponent: the one it is fighting, else one in reach in front of it. True if any.</summary>
        bool FindTargets()
        {
            targets.Clear();
            Vector3 right = AIGeometry.Right(LineFacing);
            bool any = false;
            for (int i = 0; i < active.Count; i++)
            {
                AITroopAgent a = active[i];
                IAITroop target = a.Troop.IsInMelee ? a.Troop.MeleeOpponent : null;
                if (target == null || !target.IsAlive) target = OpponentInFront(a, right);
                if (target == null)
                {
                    holdSpots.Remove(a);
                    continue;
                }
                targets[a] = target;
                any = true;
            }
            return any;
        }

        bool IsInFront(AITroopAgent agent, IAITroop opponent) =>
            IsInFront(agent, AIGeometry.Flat(opponent.Position - agent.Position), AIGeometry.Right(LineFacing));

        /// <summary>
        /// True if an opponent this far away (d, from the troop's pivot) is in front of the troop: within its side reach,
        /// and no further out to the side of its own width than it is ahead (so not at its flank).
        /// </summary>
        bool IsInFront(AITroopAgent agent, Vector3 d, Vector3 right)
        {
            float half = Width(agent) * 0.5f;
            float forward = Vector3.Dot(d, LineFacing);
            float side = Mathf.Abs(Vector3.Dot(d, right));
            return side <= half + def.attackSideReach && forward >= Mathf.Max(0f, side - half);
        }

        IAITroop OpponentInFront(AITroopAgent agent, Vector3 right)
        {
            IReadOnlyList<IAITroop> opponents = Blackboard.ActiveOpponents;
            IAITroop best = null;
            float bestScore = float.PositiveInfinity;
            for (int i = 0; i < opponents.Count; i++)
            {
                Vector3 d = AIGeometry.Flat(opponents[i].Position - agent.Position);
                float distance = d.magnitude;
                if (distance > def.attackRange || !IsInFront(agent, d, right)) continue;
                float side = Mathf.Abs(Vector3.Dot(d, right));
                float score = distance + side; // straight ahead first, so the chain does not bend
                if (score < bestScore)
                {
                    bestScore = score;
                    best = opponents[i];
                }
            }
            return best;
        }

        void Fight()
        {
            Phase = ChainPhase.Fighting;

            // The line moves up (or back) level with the troops that are fighting; the others hold their places on it.
            float shift = 0f;
            int fighting = 0;
            foreach (KeyValuePair<AITroopAgent, IAITroop> pair in targets)
            {
                shift += Vector3.Dot(AIGeometry.Flat(pair.Key.Position - LineCentre), LineFacing);
                fighting++;
            }
            if (fighting > 0 && Mathf.Abs(shift / fighting) > 1f) LineCentre += LineFacing * (shift / fighting);
            ComputeSlots();

            for (int i = 0; i < active.Count; i++)
            {
                AITroopAgent a = active[i];
                if (!targets.TryGetValue(a, out IAITroop target))
                {
                    a.HoldInLine(slots[i], LineFacing);
                    continue;
                }
                bool inMeleeWithIt = a.Troop.IsInMelee && ReferenceEquals(a.Troop.MeleeOpponent, target);
                if (inMeleeWithIt && IsInFront(a, target))
                {
                    // Stand its ground where it met the enemy: still fighting, still in the chain.
                    if (!holdSpots.TryGetValue(a, out Vector3 spot)) holdSpots[a] = spot = AIGeometry.Flat(a.Position);
                    a.HoldInLine(spot, LineFacing);
                    continue;
                }
                // Close in on the opponent in front, or turn on one at the flank or rear.
                holdSpots.Remove(a);
                a.Attack(target);
            }
            LinkNeighbours();
            State = DetachmentState.Engaged;
        }

        // Re-forms the chain where it stands, facing the target, before it goes on.
        void Regroup(bool hasTarget, Vector3 targetPosition)
        {
            holdSpots.Clear();
            LineCentre = AIGeometry.Flat(Detachment.Centre);
            if (hasTarget) LineFacing = AIGeometry.Direction(LineCentre, targetPosition, LineFacing);
            SortLeftToRight();
            Phase = ChainPhase.Forming;
            boundStarted = Blackboard.Time;
        }

        public override void DrawGizmos()
        {
            if (slots.Count == 0) return;
            Vector3 right = AIGeometry.Right(LineFacing);
            float half = 0f;
            for (int i = 0; i < active.Count && i < slots.Count; i++) half += Width(active[i]) * 0.5f;
            Gizmos.color = new Color(0.25f, 0.45f, 1f, 0.9f);
            Gizmos.DrawLine(LineCentre - right * half, LineCentre + right * half);
            Gizmos.DrawLine(LineCentre, LineCentre + LineFacing * 3f);
            for (int i = 0; i < slots.Count; i++) Gizmos.DrawWireSphere(slots[i], 0.4f);
        }
    }
}
