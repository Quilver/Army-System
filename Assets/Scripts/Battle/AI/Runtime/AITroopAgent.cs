using System;
using System.Collections.Generic;
using UnityEngine;

namespace LeveragedWars.Battle.AI
{
    [Serializable]
    public sealed class TroopReflexSettings
    {
        [Tooltip("A holding troop turns to face an enemy charging from within this distance (units).")]
        public float braceRadius = 10f;

        [Tooltip("An enemy counts as charging when closing at this fraction of its run speed.")]
        [UnityEngine.Range(0.1f, 1f)] public float chargeSpeedFraction = 0.6f;

        [Tooltip("A troop is at its hold position when within this distance (units).")]
        public float holdArrivalRadius = 1.5f;

        [Tooltip("A ranged troop keeps its current target while it stays within this multiple of its range.")]
        public float targetStickiness = 1.05f;

        [Tooltip("A melee troop sent at an enemy goes round friendly troops in its way rather than pushing through them " +
                 "(SU-179).")]
        public bool goRoundFriends = true;

        [Tooltip("How far a troop going round friends keeps its pivot from their soldiers (units). A troop already closing " +
                 "on its target only turns aside when its way passes within half of this.")]
        public float friendClearance = 1.5f;
    }

    /// <summary>
    /// Troop layer. Carries out the order its formation requests and adds reflexes:
    /// fight back in melee, go round friends on the way to a melee target, shoot while holding, face a charge.
    /// Makes no tactical choices.
    /// </summary>
    public sealed class AITroopAgent
    {
        public AITroopAgent(IAITroop troop) => Troop = troop;

        static readonly List<AIFootprint> friendFootprints = new List<AIFootprint>();

        public IAITroop Troop { get; }
        public Detachment Detachment { get; internal set; }
        /// <summary>What the formation asked for.</summary>
        public AIOrder Requested { get; private set; }
        /// <summary>What was last sent to the troop, after reflexes.</summary>
        public AIOrder Issued { get; private set; }
        /// <summary>The reflex currently overriding the requested order, for the debug overlay.</summary>
        public string Reflex { get; private set; }

        public bool IsActive => Troop.IsAlive && !Troop.IsRouting;
        public Vector3 Position => Troop.Position;

        public void Hold(Vector3 position, Vector3 facing) => Requested = AIOrder.Hold(position, facing);
        /// <summary>Hold a place in a chain (AIOrder.Line): like Hold, with no reflexes at all (no fighting back, brace or shooting).</summary>
        public void HoldInLine(Vector3 position, Vector3 facing) => Requested = AIOrder.Line(position, facing);
        public void MoveTo(Vector3 position, Vector3 facing, bool run) => Requested = AIOrder.Move(position, facing, run);
        public void Attack(IAITroop target) { if (target != null) Requested = AIOrder.Attack(target); }
        public void Charge(IAITroop target) { if (target != null) Requested = AIOrder.Charge(target); }
        public void FallBack(Vector3 position) => Requested = AIOrder.FallBack(position);

        internal void Tick(AIBlackboard blackboard, TroopReflexSettings settings)
        {
            if (!IsActive)
            {
                // Routing troops belong to the battle mechanics. Clearing Issued makes
                // the AI re-issue its order once the troop rallies.
                Issued = default;
                Reflex = null;
                return;
            }

            AIOrder effective = ApplyReflexes(blackboard, settings);
            if (effective.Kind == AIOrderKind.None || effective.IsEquivalent(Issued)) return;
            effective.IssueTo(Troop);
            Issued = effective;
        }

        AIOrder ApplyReflexes(AIBlackboard blackboard, TroopReflexSettings settings)
        {
            Reflex = null;
            AIOrder requested = Requested;

            // Fall back is never interrupted. A place in a chain (Line) is the chain formation's call, in melee too: it holds
            // the line where a troop fighting back would wheel out of it.
            if (requested.Kind == AIOrderKind.None || requested.Kind == AIOrderKind.FallBack || requested.Kind == AIOrderKind.Line)
                return requested;

            // Fight back against whatever is engaging this troop in melee.
            IAITroop opponent = Troop.IsInMelee ? Troop.MeleeOpponent : null;
            if (opponent != null && opponent.IsAlive)
            {
                if (requested.Kind == AIOrderKind.Attack && ReferenceEquals(requested.Target, opponent)) return requested;
                Reflex = "fight back";
                return AIOrder.Attack(opponent);
            }

            // Go round friends (SU-179): a melee troop whose straight way to its target crosses a friendly troop heads for
            // the corner on the shortest way round first, instead of pushing through.
            if ((requested.Kind == AIOrderKind.Attack || requested.Kind == AIOrderKind.Charge) && settings.goRoundFriends &&
                !Troop.IsRanged && TryGoRound(blackboard, requested.Target, settings, out AIOrder detour))
            {
                Reflex = "go round";
                return detour;
            }

            if (requested.Kind != AIOrderKind.Hold) return requested;
            if (AIGeometry.FlatDistance(Troop.Position, requested.Position) > settings.holdArrivalRadius) return requested;

            // Ranged troops shoot while holding.
            if (Troop.IsRanged && Troop.HasAmmo)
            {
                IAITroop target = CurrentTargetIfStillGood(settings) ?? blackboard.BestRangedTarget(Troop);
                if (target != null)
                {
                    Reflex = "firing";
                    return AIOrder.Attack(target);
                }
            }

            // Turn to face a charge.
            IAITroop charger = blackboard.IncomingCharge(Troop, settings.braceRadius, settings.chargeSpeedFraction);
            if (charger != null)
            {
                Reflex = "brace";
                return AIOrder.Hold(requested.Position,
                    AIGeometry.Direction(Troop.Position, charger.Position, requested.Facing));
            }

            return requested;
        }

        bool TryGoRound(AIBlackboard blackboard, IAITroop target, TroopReflexSettings settings, out AIOrder detour)
        {
            detour = default;
            if (target == null || !target.IsAlive) return false;
            friendFootprints.Clear();
            IReadOnlyList<IAITroop> friends = blackboard.ActiveAiTroops;
            for (int i = 0; i < friends.Count; i++)
                if (!ReferenceEquals(friends[i], Troop)) friendFootprints.Add(AIFootprint.Of(friends[i]));
            // A troop already closing on this target turns aside only when its way passes well inside the clearance, so it
            // does not flick between going round and closing in at the edge.
            bool closing = (Issued.Kind == AIOrderKind.Attack || Issued.Kind == AIOrderKind.Charge) &&
                           ReferenceEquals(Issued.Target, target);
            float clearance = closing ? settings.friendClearance * 0.5f : settings.friendClearance;
            if (!AIApproach.TryFindDetour(Troop.Position, target.Position, friendFootprints, clearance, out Vector3 waypoint))
                return false;
            detour = AIOrder.Move(waypoint, AIGeometry.Direction(waypoint, target.Position, Troop.Forward), true);
            return true;
        }

        IAITroop CurrentTargetIfStillGood(TroopReflexSettings settings)
        {
            if (Issued.Kind != AIOrderKind.Attack) return null;
            IAITroop t = Issued.Target;
            if (t == null || !t.IsAlive || t.IsRouting || t.IsInMelee) return null;
            return AIGeometry.FlatDistance(Troop.Position, t.Position) <= Troop.MaxRange * settings.targetStickiness ? t : null;
        }
    }
}
