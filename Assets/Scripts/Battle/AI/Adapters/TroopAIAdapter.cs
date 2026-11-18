using System.Collections.Generic;
using UnityEngine;

namespace LeveragedWars.Battle.AI
{
    /// <summary>
    /// The AI's view of one Troop (IAITroop). BattleAIBattlefield adds one to every troop at the start of the battle and
    /// to troops spawned later, so it does not need adding by hand.
    /// Orders map onto the troop's own orders (spec §6): Hold = a line order at the position and facing, Move and
    /// Fall back = Move, Attack and Charge = Pursue (charges are the troop's own, ChargeCoordinator). A ranged troop
    /// told to Attack walks into range of the target, then stands facing it; its RangedFireController picks what to shoot.
    /// Chains (IAIChainTroop): LinkRightTo makes the same pending order link as a multi-troop line order (ChainLinker),
    /// so the troop system activates, holds and breaks it by its own rules (spec §3.4).
    /// Footprint (IAIFootprintTroop) is the troop's outline (spec §3.3), so the AI can steer melee troops round friends.
    /// </summary>
    [DisallowMultipleComponent, RequireComponent(typeof(Troop))]
    public sealed class TroopAIAdapter : MonoBehaviour, IAITroop, IAIChainTroop, IAIFootprintTroop
    {
        /// <summary>Team id of the player's troops, used when the troop is under neither of the battle's armies.</summary>
        public static int PlayerTeamId = 0;

        const float RangedCheckInterval = 0.25f;
        const float ApproachRangeFraction = 0.85f;
        const float InRangeFraction = 1.05f; // matches the reflex target stickiness, so a holding troop never walks after its target
        const float FacingToleranceDegrees = 20f;

        Troop troop;
        IAITroop rangedTarget;
        float nextRangedCheck;
        float lastKnownRange;

        /// <summary>The troop, or null (Unity null) once it has been destroyed.</summary>
        public Troop Troop
        {
            get
            {
                // A destroyed adapter can still be reached through IAITroop; GetComponent would throw on it.
                if (troop == null && this != null) troop = GetComponent<Troop>();
                return troop;
            }
        }

        /// <summary>The adapter for a troop, added if missing.</summary>
        public static TroopAIAdapter For(Troop troop)
        {
            if (troop == null) return null;
            return troop.TryGetComponent(out TroopAIAdapter adapter) ? adapter : troop.gameObject.AddComponent<TroopAIAdapter>();
        }

        void Awake() => troop = GetComponent<Troop>();

        // ---- IAITroop: state ----

        public int Id => GetInstanceID();
        public string DisplayName => this != null ? name : "(destroyed troop)";

        public AITeam Team
        {
            get
            {
                Troop t = Troop;
                if (t == null) return AITeam.None;
                global::Battle battle = global::Battle.Instance;
                if (battle != null && t.Army != null)
                {
                    if (t.Army == battle.enemy) return AITeam.Enemy;
                    if (t.Army == battle.player) return AITeam.Player;
                }
                return t.TeamId == PlayerTeamId ? AITeam.Player : AITeam.Enemy;
            }
        }

        public bool IsAlive
        {
            get
            {
                Troop t = Troop;
                return t != null && !t.IsDefeated && t.LivingSoldierCount > 0;
            }
        }

        // Routed troops never recover (spec §5.4). A retreating troop (fault slip) or a disordered one (SU-104) ignores
        // orders until it recovers, so the AI treats it as routing and re-issues its order afterwards.
        public bool IsRouting
        {
            get
            {
                Troop t = Troop;
                return t != null && (t.IsRouted || t.IsRetreating || t.IsDisordered);
            }
        }

        public Vector3 Position => Troop != null ? Troop.transform.position : Vector3.zero;
        public Vector3 Forward => Troop != null ? (Vector3)Troop.Facing : Vector3.right;
        public Vector3 Velocity => Troop != null ? (Vector3)Troop.PivotVelocity : Vector3.zero;
        public float Frontage => Troop != null ? Troop.LineLength : 0f;
        // Troops have one speed (Move); there is no separate run speed.
        public float RunSpeed => Troop != null ? Troop.PivotSpeed : 0f;

        // A ranged troop stands off and shoots: a living soldier carries a bow or crossbow. Javelin-class weapons (javelin,
        // pilum, throwing axe) are thrown before contact by infantry, so a troop carrying only those fights as melee; its
        // RangedFireController still throws them by itself.
        public bool IsRanged
        {
            get
            {
                Troop t = Troop;
                if (t == null) return false;
                IReadOnlyList<SoldierAgentState> soldiers = t.Soldiers;
                for (int i = 0; i < soldiers.Count; i++)
                    if (soldiers[i].IsAlive && IsMissileWeapon(soldiers[i].Character?.RangedWeapon)) return true;
                return false;
            }
        }

        /// <summary>True for a ranged weapon a troop stands off to shoot with (bow, crossbow), false for thrown weapons.</summary>
        public static bool IsMissileWeapon(RangedWeaponProfile weapon) =>
            weapon != null && weapon.RangedClass != RangedWeaponClass.Javelin;

        public float MaxRange
        {
            get
            {
                if (!IsRanged || Troop.RangedFire == null) return 0f;
                // MaxWeaponRange counts only soldiers free to shoot now, so remember the last non-zero value.
                float range = Troop.RangedFire.MaxWeaponRange();
                if (range > 0f) lastKnownRange = range;
                return lastKnownRange;
            }
        }

        // A ranged troop with a living bow or crossbow soldier that still has ammunition (SU-151; always, while
        // ammunition is unlimited). Out of ammunition, the ranged formations treat it as a melee troop.
        public bool HasAmmo
        {
            get
            {
                Troop t = Troop;
                if (t == null) return false;
                IReadOnlyList<SoldierAgentState> soldiers = t.Soldiers;
                for (int i = 0; i < soldiers.Count; i++)
                {
                    SoldierAgentState soldier = soldiers[i];
                    if (soldier.IsAlive && IsMissileWeapon(soldier.Character?.RangedWeapon) && soldier.HasAmmunition)
                        return true;
                }
                return false;
            }
        }

        public bool IsInMelee => Troop != null && (Troop.IsEngaged || Troop.InMelee);

        public IAITroop MeleeOpponent
        {
            get
            {
                if (!IsInMelee) return null;
                Troop enemy = Troop.EngagedEnemy;
                return enemy != null ? For(enemy) : null;
            }
        }

        // ---- IAITroop: orders ----

        public void OrderHold(Vector3 position, Vector3 facing)
        {
            rangedTarget = null;
            if (Troop == null) return;
            // The AI plans pivot positions; the ordered line is the front rank, half the rank depth ahead (SU-161).
            Vector2 lineFacing = Facing2D(facing);
            Troop.OrderLine(Troop.LineCenterAhead(position, lineFacing, Troop.NaturalLineLength), lineFacing,
                Troop.NaturalLineLength);
        }

        public void OrderMove(Vector3 position, Vector3 facing, bool run)
        {
            rangedTarget = null;
            if (Troop == null) return;
            Troop.MoveTo(position);
        }

        public void OrderAttack(IAITroop target)
        {
            rangedTarget = null;
            if (Troop == null || target == null) return;
            if (IsRanged)
            {
                rangedTarget = target;
                nextRangedCheck = 0f;
                UpdateRangedAttack();
                return;
            }
            Pursue(target);
        }

        public void OrderCharge(IAITroop target)
        {
            rangedTarget = null;
            if (Troop == null || target == null) return;
            Pursue(target);
        }

        public void OrderFallBack(Vector3 position)
        {
            rangedTarget = null;
            if (Troop == null) return;
            Troop.MoveTo(position);
        }

        void Pursue(IAITroop target)
        {
            Troop other = (target as TroopAIAdapter)?.Troop;
            if (other != null) Troop.Pursue(other);
        }

        // ---- IAIChainTroop: chains of linked battle lines (spec §3.4) ----

        public float NaturalLineLength => Troop != null ? Troop.NaturalLineLength : 0f;

        public bool HasLineOrder => Troop != null && Troop.HasManualLineOrder;

        public AIChainLink RightLinkTo(IAIChainTroop other)
        {
            Troop self = Troop;
            Troop neighbour = (other as TroopAIAdapter)?.Troop;
            if (self == null || neighbour == null) return AIChainLink.None;
            TroopEndpointLink link = self.GetLineLink(TroopConstants.RightEnd);
            if (link == null || link.Other != neighbour || link.OtherEnd != TroopConstants.LeftEnd) return AIChainLink.None;
            return link.Pending ? AIChainLink.Pending : AIChainLink.Linked;
        }

        public bool LinkRightTo(IAIChainTroop other)
        {
            Troop self = Troop;
            Troop neighbour = (other as TroopAIAdapter)?.Troop;
            if (self == null || neighbour == null || !self.HasManualLineOrder || !neighbour.HasManualLineOrder) return false;
            return ChainLinker.LinkFromOrder(self, TroopConstants.RightEnd, neighbour, TroopConstants.LeftEnd);
        }

        // ---- IAIFootprintTroop: the ground the troop stands on (SU-179) ----

        public AIFootprint Footprint
        {
            get
            {
                Troop t = Troop;
                if (t == null) return new AIFootprint(Position, Forward, 0f, 0f);
                // The outline (the formation the soldiers are steered into) rather than the footprint fitted to the soldiers:
                // it is steady in melee, where one soldier knocked out of place would stretch the footprint.
                TroopFootprint f = t.Outline;
                return new AIFootprint(f.Center, f.Facing, f.OuterHalfLength, f.OuterHalfWidth);
            }
        }

        // ---- Ranged attack: close to range, then stand facing the target ----

        void Update()
        {
            if (rangedTarget != null) UpdateRangedAttack();
        }

        void UpdateRangedAttack()
        {
            if (Time.time < nextRangedCheck) return;
            nextRangedCheck = Time.time + RangedCheckInterval;

            Troop self = Troop;
            if (self == null || !IsAlive || IsRouting || rangedTarget == null || !rangedTarget.IsAlive || rangedTarget.IsRouting)
            {
                rangedTarget = null;
                return;
            }

            float range = MaxRange;
            Vector2 position = self.transform.position;
            Vector2 toTarget = (Vector2)rangedTarget.Position - position;
            float distance = toTarget.magnitude;
            Vector2 direction = distance > 0.001f ? toTarget / distance : self.Facing;

            if (range > 0f && distance > range * InRangeFraction)
            {
                Vector2 spot = (Vector2)rangedTarget.Position - direction * (range * ApproachRangeFraction);
                if (!self.HasManualMoveOrder || (self.Destination - spot).sqrMagnitude > 1f) self.MoveTo(spot);
                return;
            }

            // In range: stop and face the target. Re-order only when moving or facing too far off, so the line is not
            // reset every check.
            if (self.IsMoving || Vector2.Angle(self.Facing, direction) > FacingToleranceDegrees)
                self.OrderLine(self.LineCenterAhead(position, direction, self.NaturalLineLength), direction,
                    self.NaturalLineLength);
        }

        static Vector2 Facing2D(Vector3 facing)
        {
            Vector2 f = facing;
            return f.sqrMagnitude > 1e-6f ? f.normalized : Vector2.right;
        }
    }
}
