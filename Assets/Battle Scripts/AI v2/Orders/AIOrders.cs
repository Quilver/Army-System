using System.Collections.Generic;
using UnityEngine;

namespace ArmySystem.AI.V2
{
    public interface IUnitOrderService
    {
        void Move(IUnit unit, Vector2 position);
        void MoveAndFace(IUnit unit, Vector2 position, Vector2 facePoint);
        void Charge(IUnit unit, IUnit enemy);
        void Target(IUnit unit, IUnit enemy);
        void Hold(IUnit unit);
        void MoveFormation(IAISquad squad, Vector2 center, Vector2? facePoint = null);
        void Charge(IAISquad squad, IUnit enemy);
        void Hold(IAISquad squad);
        void HoldAndTarget(IAISquad squad, IUnit enemy);
    }

    public readonly struct FormationSlot
    {
        public FormationSlot(Vector2 position, Vector2 facePoint)
        {
            Position = position;
            FacePoint = facePoint;
        }

        public Vector2 Position { get; }
        public Vector2 FacePoint { get; }
    }

    public interface IFormationPlanner
    {
        IReadOnlyDictionary<IUnit, FormationSlot> AssignSlots(
            IAISquad squad,
            Vector2 center,
            Vector2 facing);
    }

    public interface ITargetSelector
    {
        UnitSnapshot NearestThreat(IAISquad squad, AIContext context);
        UnitSnapshot BestTarget(IAISquad squad, ForceGroup group, AIContext context);
    }
}
