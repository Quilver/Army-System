using System;
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
    }

    /// <summary>
    /// Troop layer. Carries out the order its formation requests and adds reflexes:
    /// fight back in melee, shoot while holding, face a charge. Makes no tactical choices.
    /// </summary>
    public sealed class AITroopAgent
    {
        public AITroopAgent(IAITroop troop) => Troop = troop;

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

            // Fall back is never interrupted.
            if (requested.Kind == AIOrderKind.None || requested.Kind == AIOrderKind.FallBack) return requested;

            // Fight back against whatever is engaging this troop in melee.
            IAITroop opponent = Troop.IsInMelee ? Troop.MeleeOpponent : null;
            if (opponent != null && opponent.IsAlive)
            {
                if (requested.Kind == AIOrderKind.Attack && ReferenceEquals(requested.Target, opponent)) return requested;
                Reflex = "fight back";
                return AIOrder.Attack(opponent);
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

        IAITroop CurrentTargetIfStillGood(TroopReflexSettings settings)
        {
            if (Issued.Kind != AIOrderKind.Attack) return null;
            IAITroop t = Issued.Target;
            if (t == null || !t.IsAlive || t.IsRouting || t.IsInMelee) return null;
            return AIGeometry.FlatDistance(Troop.Position, t.Position) <= Troop.MaxRange * settings.targetStickiness ? t : null;
        }
    }
}
