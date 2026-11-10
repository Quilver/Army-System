using UnityEngine;

namespace LeveragedWars.Battle.AI
{
    public enum AIOrderKind { None, Hold, Move, Attack, Charge, FallBack }

    /// <summary>
    /// An order the AI wants a troop to carry out. Orders are compared before issuing,
    /// so troops are not re-ordered every tick.
    /// </summary>
    public readonly struct AIOrder
    {
        const float PositionTolerance = 0.75f;
        const float FacingToleranceDegrees = 12f;

        public readonly AIOrderKind Kind;
        public readonly Vector3 Position;
        public readonly Vector3 Facing;
        public readonly IAITroop Target;
        public readonly bool Run;

        AIOrder(AIOrderKind kind, Vector3 position, Vector3 facing, IAITroop target, bool run)
        {
            Kind = kind;
            Position = position;
            Facing = facing;
            Target = target;
            Run = run;
        }

        public static AIOrder Hold(Vector3 position, Vector3 facing) =>
            new AIOrder(AIOrderKind.Hold, position, AIGeometry.FlatForward(facing), null, false);

        public static AIOrder Move(Vector3 position, Vector3 facing, bool run) =>
            new AIOrder(AIOrderKind.Move, position, AIGeometry.FlatForward(facing), null, run);

        public static AIOrder Attack(IAITroop target) =>
            new AIOrder(AIOrderKind.Attack, Vector3.zero, Vector3.zero, target, false);

        public static AIOrder Charge(IAITroop target) =>
            new AIOrder(AIOrderKind.Charge, Vector3.zero, Vector3.zero, target, true);

        /// <summary>Withdraw to a point without routing. Facing is set by the Hold that follows.</summary>
        public static AIOrder FallBack(Vector3 position) =>
            new AIOrder(AIOrderKind.FallBack, position, Vector3.zero, null, true);

        public bool IsEquivalent(in AIOrder other)
        {
            if (Kind != other.Kind) return false;
            switch (Kind)
            {
                case AIOrderKind.None:
                    return true;
                case AIOrderKind.Attack:
                case AIOrderKind.Charge:
                    return ReferenceEquals(Target, other.Target);
                case AIOrderKind.FallBack:
                    return CloseTo(other.Position);
                default:
                    return Run == other.Run && CloseTo(other.Position)
                        && Vector3.Angle(Facing, other.Facing) < FacingToleranceDegrees;
            }
        }

        bool CloseTo(Vector3 p) =>
            AIGeometry.Flat(Position - p).sqrMagnitude < PositionTolerance * PositionTolerance;

        public void IssueTo(IAITroop troop)
        {
            switch (Kind)
            {
                case AIOrderKind.Hold: troop.OrderHold(Position, Facing); break;
                case AIOrderKind.Move: troop.OrderMove(Position, Facing, Run); break;
                case AIOrderKind.Attack: troop.OrderAttack(Target); break;
                case AIOrderKind.Charge: troop.OrderCharge(Target); break;
                case AIOrderKind.FallBack: troop.OrderFallBack(Position); break;
            }
        }

        public override string ToString() => Kind switch
        {
            AIOrderKind.None => "None",
            AIOrderKind.Attack or AIOrderKind.Charge => $"{Kind} {Target?.DisplayName}",
            _ => $"{Kind} {Position}"
        };
    }
}
