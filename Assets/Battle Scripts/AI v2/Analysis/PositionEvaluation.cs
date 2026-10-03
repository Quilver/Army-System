using UnityEngine;

namespace ArmySystem.AI.V2
{
    public interface IPositionEvaluator
    {
        PositionProposal FindFiringPosition(
            IAISquad squad,
            UnitSnapshot target,
            AIContext context);

        PositionProposal FindFlankPosition(
            IAISquad squad,
            ForceGroup target,
            FlankSide side,
            AIContext context);

        PositionProposal FindRetreatPosition(IAISquad squad, AIContext context);
        bool IsBehindTarget(Vector2 position, UnitSnapshot target);
    }

    public readonly struct PositionProposal
    {
        public PositionProposal(Vector2 position, float utility, bool isReachable)
        {
            Position = position;
            Utility = utility;
            IsReachable = isReachable;
        }

        public Vector2 Position { get; }
        public float Utility { get; }
        public bool IsReachable { get; }
    }
}
