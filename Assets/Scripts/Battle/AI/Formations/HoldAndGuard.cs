using System;
using System.Collections.Generic;
using UnityEngine;

namespace LeveragedWars.Battle.AI
{
    // ---------------------------------------------------------------- Hold

    [Serializable, AddTypeMenu("Hold")]
    public sealed class HoldDefinition : FormationDefinition
    {
        [Tooltip("Where the detachment holds.")]
        public AnchorReference anchor = new AnchorReference();

        [Tooltip("Engage enemies within this distance of the anchor (units).")]
        public float aggroRange = 8f;

        [Tooltip("Return to the anchor when further than this from it (units).")]
        public float leashRange = 14f;

        [Tooltip("Gap between troops in the line (units).")]
        public float lineGap = 2f;

        public override Formation Create() => new HoldFormation(this);
    }

    /// <summary>Stay at the anchor; engage only what enters aggro range; return when past the leash.</summary>
    public sealed class HoldFormation : AnchoredFormation
    {
        readonly HoldDefinition def;

        public HoldFormation(HoldDefinition definition) => def = definition;

        protected override float LeashRange => def.leashRange;
        protected override float LineGap => def.lineGap;

        protected override void ResolveAnchor(out Vector3 position, out Vector3 facing) =>
            def.anchor.TryResolve(Detachment, Blackboard, out position, out facing);

        protected override void FindThreats(List<IAITroop> results) =>
            Blackboard.OpponentsWithin(AnchorPosition, def.aggroRange, results);

        public override void DrawGizmos()
        {
            base.DrawGizmos();
            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.7f);
            AIGizmos.Circle(AnchorPosition, def.aggroRange);
        }
    }

    // ---------------------------------------------------------------- Guard

    [Serializable, AddTypeMenu("Guard")]
    public sealed class GuardDefinition : FormationDefinition
    {
        [Tooltip("What is being protected: a detachment, objective or marker.")]
        public AnchorReference guarded = new AnchorReference { kind = AnchorKind.Detachment };

        [Tooltip("Where the guards stand. Defaults to where they start.")]
        public AnchorReference guardPosition = new AnchorReference();

        [Tooltip("Intercept enemies within this distance of the guarded target or of the guard position (units).")]
        public float aggroRange = 10f;

        [Tooltip("Return to the guard position when further than this from it (units).")]
        public float leashRange = 16f;

        [Tooltip("Gap between troops in the line (units).")]
        public float lineGap = 2f;

        public override Formation Create() => new GuardFormation(this);
    }

    /// <summary>Protect a troop, detachment or objective; intercept any enemy within aggro range of it.</summary>
    public sealed class GuardFormation : AnchoredFormation
    {
        readonly GuardDefinition def;
        Vector3 guardedPosition;
        bool hasGuarded;

        public GuardFormation(GuardDefinition definition) => def = definition;

        protected override float LeashRange => def.leashRange;
        protected override float LineGap => def.lineGap;

        protected override void ResolveAnchor(out Vector3 position, out Vector3 facing)
        {
            def.guardPosition.TryResolve(Detachment, Blackboard, out position, out facing);
            hasGuarded = def.guarded.TryResolve(Detachment, Blackboard, out guardedPosition, out _);
        }

        protected override void FindThreats(List<IAITroop> results)
        {
            Blackboard.OpponentsWithin(AnchorPosition, def.aggroRange, results);
            if (hasGuarded) Blackboard.OpponentsWithin(guardedPosition, def.aggroRange, results, append: true);
        }

        public override void DrawGizmos()
        {
            base.DrawGizmos();
            if (!hasGuarded) return;
            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.7f);
            AIGizmos.Circle(guardedPosition, def.aggroRange);
            Gizmos.DrawLine(AnchorPosition, guardedPosition);
        }
    }
}
