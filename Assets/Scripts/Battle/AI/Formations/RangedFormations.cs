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

        public override Formation Create() => new SkirmishFormation(this);
    }

    /// <summary>
    /// Ranged harassment: advance to firing range of the nearest enemy, fall back to a safe point
    /// when enemies close in, then repeat.
    /// </summary>
    public sealed class SkirmishFormation : Formation
    {
        readonly SkirmishDefinition def;
        readonly HashSet<AITroopAgent> fallingBack = new HashSet<AITroopAgent>();
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
                    if (ReturnToFallback(a, toFallback, faceEnemy)) retreating = true;
                    continue;
                }

                if (nearestDistance < def.minimumRange)
                    fallingBack.Add(a);
                else if (toFallback <= ArrivalRadius * 2f && nearestDistance > def.minimumRange * def.resumeMultiplier)
                    fallingBack.Remove(a);

                if (fallingBack.Contains(a))
                {
                    if (ReturnToFallback(a, toFallback, faceEnemy)) retreating = true;
                    else if (a.Issued.Kind == AIOrderKind.Attack) engaged = true;
                    continue;
                }

                IAITroop target = Blackboard.NearestOpponent(fallback, def.maxDistanceFromFallback + a.Troop.MaxRange);
                if (target == null)
                {
                    if (ReturnToFallback(a, toFallback, fallbackFacing)) moving = true;
                    continue;
                }

                // Stand at preferred range from the target, but no further than the leash from the fallback point.
                Vector3 away = AIGeometry.Direction(target.Position, a.Position, -fallbackFacing);
                Vector3 spot = target.Position + away * (a.Troop.MaxRange * def.preferredRangeFraction);
                Vector3 offset = AIGeometry.Flat(spot - fallback);
                if (offset.magnitude > def.maxDistanceFromFallback)
                    spot = fallback + offset.normalized * def.maxDistanceFromFallback;

                Vector3 face = AIGeometry.Direction(spot, target.Position, fallbackFacing);
                if (FormationUtility.MoveOrHold(a, spot, face, false, ArrivalRadius)) moving = true;
                else if (a.Issued.Kind == AIOrderKind.Attack) engaged = true;
            }

            State = engaged ? DetachmentState.Engaged
                : retreating ? DetachmentState.Returning
                : moving ? DetachmentState.Moving
                : DetachmentState.Waiting;
        }

        /// <summary>Falls back to the safe point, or holds there once arrived. Returns true while travelling.</summary>
        bool ReturnToFallback(AITroopAgent agent, float toFallback, Vector3 facing)
        {
            if (toFallback > ArrivalRadius)
            {
                agent.FallBack(fallback);
                return true;
            }
            agent.Hold(fallback, facing);
            return false;
        }

        public override void Exit() => fallingBack.Clear();

        public override void DrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.8f);
            Gizmos.DrawWireCube(fallback, Vector3.one);
            AIGizmos.Circle(fallback, def.maxDistanceFromFallback);
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
