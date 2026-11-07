using System;
using System.Collections.Generic;
using UnityEngine;

namespace LeveragedWars.Battle.AI
{
    // ---------------------------------------------------------------- Skirmish

    [Serializable, AddTypeMenu("Skirmish")]
    public sealed class SkirmishDefinition : FormationDefinition
    {
        [Tooltip("The safe point to fall back to, e.g. between the guarding infantry.")]
        public AnchorReference fallbackPoint = new AnchorReference { kind = AnchorKind.Transform };

        [Tooltip("Shoot from this fraction of maximum range.")]
        [UnityEngine.Range(0.5f, 1f)] public float preferredRangeFraction = 0.85f;

        [Tooltip("Fall back when an enemy is closer than this (units). Keep it well below the preferred range.")]
        public float minimumRange = 6f;

        [Tooltip("Start harassing again once back at the fallback point and the nearest enemy is this many times the minimum range away.")]
        public float resumeMultiplier = 1.5f;

        [Tooltip("Never stand further than this from the fallback point (units).")]
        public float maxDistanceFromFallback = 30f;

        [Tooltip("Gap (units) a fall-back route keeps from the nearest enemy, on top of both troops' half-widths. A route " +
                 "passing closer goes round the enemy by an escape spot first.")]
        public float routeClearanceMargin = 2f;

        [Tooltip("When the way to the fallback point is blocked by the enemy, first move this far away from it (units), " +
                 "within the leash.")]
        public float escapeDistance = 8f;

        public override Formation Create() => new SkirmishFormation(this);
    }

    /// <summary>
    /// Ranged harassment: advance to firing range of the nearest enemy, fall back to a safe point
    /// when enemies close in, then repeat.
    /// Falling back never routes through or into the threat (the nearest enemy): a troop locked in melee stands and
    /// fights (morale decides), and when the straight route to the fallback point passes within the clearance of the
    /// threat (both troops' half-widths + Route Clearance Margin), the troop first moves Escape Distance away from it,
    /// inside the leash and on the side that keeps clear of it, then heads for the fallback point once the way is clear.
    /// Walking out to firing range never passes through an enemy either: the troop stands and shoots instead.
    /// </summary>
    public sealed class SkirmishFormation : Formation
    {
        // Headings tried for the escape spot, in degrees from straight away from the threat, in order of preference.
        static readonly float[] EscapeHeadingsDegrees = { 0f, 45f, -45f, 90f, -90f };
        // Once escaping, the route must clear the threat by this much more (units) before the troop heads for the
        // fallback point again, so it does not flicker between the two at the edge of the clearance.
        const float RouteReleaseMargin = 1f;
        // Inside the clearance, a route counts as pressing into the threat only if it comes this much closer (units)
        // than the troop stands now; walking straight away from a close threat is allowed.
        const float ApproachTolerance = 0.1f;

        enum Withdrawal { Holding, Travelling, Fighting }

        readonly SkirmishDefinition def;
        readonly HashSet<AITroopAgent> fallingBack = new HashSet<AITroopAgent>();
        readonly Dictionary<AITroopAgent, Vector3> escapeSpots = new Dictionary<AITroopAgent, Vector3>();
        Vector3 fallback;
        Vector3 fallbackFacing;

        public SkirmishFormation(SkirmishDefinition definition) => def = definition;

        public override void Tick()
        {
            def.fallbackPoint.TryResolve(Detachment, Blackboard, out fallback, out fallbackFacing);
            bool engaged = false, moving = false, retreating = false;

            foreach (AITroopAgent a in Troops)
            {
                IAITroop nearest = Blackboard.NearestOpponent(a.Position);
                float nearestDistance = nearest != null ? AIGeometry.FlatDistance(a.Position, nearest.Position) : float.PositiveInfinity;
                Vector3 faceEnemy = nearest != null ? AIGeometry.Direction(a.Position, nearest.Position, fallbackFacing) : fallbackFacing;
                float toFallback = AIGeometry.FlatDistance(a.Position, fallback);

                // Melee troops, and ranged troops out of ammunition, stay at the fallback point.
                if (!a.Troop.IsRanged || !a.Troop.HasAmmo)
                {
                    Withdrawal w = Withdraw(a, nearest, faceEnemy);
                    if (w == Withdrawal.Travelling) retreating = true;
                    else if (w == Withdrawal.Fighting) engaged = true;
                    continue;
                }

                if (nearestDistance < def.minimumRange)
                    fallingBack.Add(a);
                else if (toFallback <= ArrivalRadius * 2f && nearestDistance > def.minimumRange * def.resumeMultiplier)
                    fallingBack.Remove(a);

                if (fallingBack.Contains(a))
                {
                    Withdrawal w = Withdraw(a, nearest, faceEnemy);
                    if (w == Withdrawal.Travelling) retreating = true;
                    else if (w == Withdrawal.Fighting || a.Issued.Kind == AIOrderKind.Attack) engaged = true;
                    continue;
                }

                IAITroop target = Blackboard.NearestOpponent(fallback, def.maxDistanceFromFallback + a.Troop.MaxRange);
                if (target == null)
                {
                    Withdrawal w = Withdraw(a, nearest, fallbackFacing);
                    if (w == Withdrawal.Travelling) moving = true;
                    else if (w == Withdrawal.Fighting) engaged = true;
                    continue;
                }

                // Stand at preferred range from the target, but no further than the leash from the fallback point.
                escapeSpots.Remove(a);
                Vector3 away = AIGeometry.Direction(target.Position, a.Position, -fallbackFacing);
                Vector3 spot = AIGeometry.ClampToCircle(target.Position + away * (a.Troop.MaxRange * def.preferredRangeFraction),
                    fallback, def.maxDistanceFromFallback);

                // The leash can put the spot on the far side of an enemy: never walk out through it, stand and shoot.
                if (nearest != null && RouteBlocked(a, nearest, spot, 0f))
                {
                    a.Hold(a.Position, faceEnemy);
                    if (a.Troop.IsInMelee || a.Issued.Kind == AIOrderKind.Attack) engaged = true;
                    continue;
                }

                Vector3 face = AIGeometry.Direction(spot, target.Position, fallbackFacing);
                if (FormationUtility.MoveOrHold(a, spot, face, false, ArrivalRadius)) moving = true;
                else if (a.Issued.Kind == AIOrderKind.Attack) engaged = true;
            }

            State = engaged ? DetachmentState.Engaged
                : retreating ? DetachmentState.Returning
                : moving ? DetachmentState.Moving
                : DetachmentState.Waiting;
        }

        /// <summary>
        /// Goes back to the safe point without routing through or into the threat. In melee: stand and fight. Route to
        /// the fallback point blocked by the threat: head for an escape spot first. Otherwise fall back, or hold once there.
        /// </summary>
        Withdrawal Withdraw(AITroopAgent agent, IAITroop threat, Vector3 facing)
        {
            // Turning away in melee only presses the troop into its attacker: fight back and let morale decide.
            if (agent.Troop.IsInMelee)
            {
                escapeSpots.Remove(agent);
                IAITroop opponent = agent.Troop.MeleeOpponent;
                if (FormationUtility.IsValidTarget(opponent)) agent.Attack(opponent);
                else agent.Hold(agent.Position, facing);
                return Withdrawal.Fighting;
            }

            bool escaping = escapeSpots.ContainsKey(agent);
            if (threat == null || !RouteBlocked(agent, threat, fallback, escaping ? RouteReleaseMargin : 0f))
            {
                escapeSpots.Remove(agent);
                return ReturnToFallback(agent, facing) ? Withdrawal.Travelling : Withdrawal.Holding;
            }

            // Keep the escape spot chosen earlier while it is still short of reached and its way stays clear.
            if (!escapeSpots.TryGetValue(agent, out Vector3 spot)
                || AIGeometry.FlatDistance(agent.Position, spot) <= ArrivalRadius
                || RouteBlocked(agent, threat, spot, 0f))
            {
                if (!TryFindEscapeSpot(agent, threat, out spot))
                {
                    // Cornered against the leash: face the threat where it stands rather than walk into it.
                    escapeSpots.Remove(agent);
                    agent.Hold(agent.Position, AIGeometry.Direction(agent.Position, threat.Position, facing));
                    return Withdrawal.Holding;
                }
                escapeSpots[agent] = spot;
            }

            agent.FallBack(spot);
            return Withdrawal.Travelling;
        }

        /// <summary>Falls back to the safe point, or holds there once arrived. Returns true while travelling.</summary>
        bool ReturnToFallback(AITroopAgent agent, Vector3 facing)
        {
            if (AIGeometry.FlatDistance(agent.Position, fallback) > ArrivalRadius)
            {
                agent.FallBack(fallback);
                return true;
            }
            agent.Hold(fallback, facing);
            return false;
        }

        /// <summary>How far (units) a route keeps from the threat: both troops' half-widths plus the margin.</summary>
        float Clearance(AITroopAgent agent, IAITroop threat) =>
            (agent.Troop.Frontage + threat.Frontage) * 0.5f + def.routeClearanceMargin;

        /// <summary>
        /// True when walking straight from the troop to the destination passes within the clearance (plus extra) of the
        /// threat, i.e. through it or into it. A route that only keeps or opens the current distance is never blocked.
        /// </summary>
        bool RouteBlocked(AITroopAgent agent, IAITroop threat, Vector3 destination, float extra)
        {
            float closest = AIGeometry.DistanceToSegment(threat.Position, agent.Position, destination);
            float now = AIGeometry.FlatDistance(agent.Position, threat.Position);
            return closest < Clearance(agent, threat) + extra && closest < now - ApproachTolerance;
        }

        /// <summary>
        /// A spot Escape Distance away from the threat, clamped to the leash, whose way there keeps clear of the threat.
        /// Prefers spots from which the way on to the fallback point is clear (the nearest to it); otherwise the spot
        /// furthest from the threat. False when every heading is blocked.
        /// </summary>
        bool TryFindEscapeSpot(AITroopAgent agent, IAITroop threat, out Vector3 best)
        {
            best = agent.Position;
            bool found = false, bestClear = false;
            float bestScore = float.NegativeInfinity;
            float clearance = Clearance(agent, threat);
            Vector3 away = AIGeometry.Direction(threat.Position, agent.Position, -fallbackFacing);

            foreach (float turn in EscapeHeadingsDegrees)
            {
                Vector3 candidate = AIGeometry.ClampToCircle(agent.Position + AIGeometry.Rotate(away, turn) * def.escapeDistance,
                    fallback, def.maxDistanceFromFallback);
                if (AIGeometry.FlatDistance(agent.Position, candidate) <= ArrivalRadius) continue;
                if (RouteBlocked(agent, threat, candidate, 0f)) continue;

                bool clear = AIGeometry.DistanceToSegment(threat.Position, candidate, fallback) >= clearance;
                float score = clear ? -AIGeometry.FlatDistance(candidate, fallback) : AIGeometry.FlatDistance(candidate, threat.Position);
                if (found && ((bestClear && !clear) || (bestClear == clear && score <= bestScore))) continue;
                found = true;
                bestClear = clear;
                bestScore = score;
                best = candidate;
            }
            return found;
        }

        public override void Exit()
        {
            fallingBack.Clear();
            escapeSpots.Clear();
        }

        public override void DrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.8f);
            Gizmos.DrawWireCube(fallback, Vector3.one);
            AIGizmos.Circle(fallback, def.maxDistanceFromFallback);
            foreach (KeyValuePair<AITroopAgent, Vector3> escape in escapeSpots)
            {
                Gizmos.DrawLine(escape.Key.Position, escape.Value);
                Gizmos.DrawWireSphere(escape.Value, 0.5f);
            }
        }
    }

    // ---------------------------------------------------------------- Support fire

    [Serializable, AddTypeMenu("Support Fire")]
    public sealed class SupportFireDefinition : FormationDefinition
    {
        [Tooltip("The friendly detachment to support.")]
        public string supportedDetachmentId;

        [Tooltip("Stand this far behind the supported detachment (units). Negative stands in front of it.")]
        public float standOff = 6f;

        [Tooltip("Gap between troops in the line (units).")]
        public float lineGap = 2f;

        public override Formation Create() => new SupportFireFormation(this);
    }

    /// <summary>
    /// Ranged troops stay leashed to a friendly detachment and shoot enemies approaching it.
    /// Enemies already in melee are left alone to avoid hitting friendly troops.
    /// </summary>
    public sealed class SupportFireFormation : Formation
    {
        readonly SupportFireDefinition def;
        readonly List<LineSlot> slots = new List<LineSlot>();
        Vector3 centre;
        Vector3 supportedCentre;

        public SupportFireFormation(SupportFireDefinition definition) => def = definition;

        public override void Tick()
        {
            Detachment supported = Blackboard.GetDetachment(def.supportedDetachmentId);
            Vector3 facing;
            if (supported != null && supported.ActiveAgents.Count > 0)
            {
                supportedCentre = supported.Centre;
                facing = supported.Facing;
                centre = supportedCentre - facing * def.standOff;
            }
            else
            {
                // Nothing left to support: hold where we are and keep shooting.
                supportedCentre = Detachment.Centre;
                facing = Detachment.Facing;
                centre = Detachment.Centre;
            }

            FormationUtility.ComputeLine(Troops, centre, facing, def.lineGap, slots);
            bool engaged = false, moving = false;
            foreach (LineSlot slot in slots)
            {
                AITroopAgent a = slot.Agent;
                if (FormationUtility.MoveOrHold(a, slot.Position, facing, false, ArrivalRadius))
                {
                    moving = true;
                    continue;
                }
                if (!a.Troop.IsRanged || !a.Troop.HasAmmo) continue;

                IAITroop target = TargetFor(a);
                if (target != null)
                {
                    a.Attack(target);
                    engaged = true;
                }
            }

            State = engaged ? DetachmentState.Engaged : moving ? DetachmentState.Moving : DetachmentState.Waiting;
        }

        /// <summary>The in-range enemy closest to the supported detachment, keeping the current target while it stays valid.</summary>
        IAITroop TargetFor(AITroopAgent agent)
        {
            float range = agent.Troop.MaxRange;
            if (agent.Issued.Kind == AIOrderKind.Attack)
            {
                IAITroop current = agent.Issued.Target;
                if (FormationUtility.IsValidTarget(current) && !current.IsInMelee
                    && AIGeometry.FlatDistance(agent.Position, current.Position) <= range)
                    return current;
            }

            IAITroop best = null;
            float bestScore = float.PositiveInfinity;
            IReadOnlyList<IAITroop> opponents = Blackboard.ActiveOpponents;
            for (int i = 0; i < opponents.Count; i++)
            {
                IAITroop o = opponents[i];
                if (o.IsInMelee) continue;
                if (AIGeometry.FlatDistance(agent.Position, o.Position) > range) continue;
                float score = AIGeometry.FlatDistance(supportedCentre, o.Position);
                if (score < bestScore) { bestScore = score; best = o; }
            }
            return best;
        }

        public override void DrawGizmos()
        {
            Gizmos.color = new Color(0.6f, 0.9f, 0.3f, 0.8f);
            Gizmos.DrawLine(centre, supportedCentre);
        }
    }
}
