using System.Collections.Generic;
using UnityEngine;

namespace ArmySystem.AI.V2
{
    public interface IAISquad
    {
        string Id { get; }
        AISquadDefinition Definition { get; }
        IReadOnlyList<IUnit> LivingUnits { get; }
        ISquadTask ActiveTask { get; }
        SquadRuntimeState State { get; }
        Vector2 Center { get; }
        Vector2 Facing { get; }
        float Strength { get; }

        void AssignTask(ISquadTask task, SquadTaskContext context);
        void CancelTask(SquadTaskContext context);
        void Tick(SquadTaskContext context);
    }

    public sealed class SquadRuntimeState
    {
        public bool IsEngaged { get; set; }
        public bool IsRegrouping { get; set; }
        public bool HasReachedDestination { get; set; }
        public IUnit CurrentTarget { get; set; }
        public ForceGroup TargetGroup { get; set; }
        public Vector2 CurrentAnchor { get; set; }
        public float CasualtyRatio { get; set; }
        public TaskStatus TaskStatus { get; set; }
    }

    public sealed class TacticalAssignment
    {
        public string Id { get; set; }
        public IAISquad Squad { get; set; }
        public SquadTaskDefinition Task { get; set; }
        public IAIObjective Objective { get; set; }
        public int Priority { get; set; }
    }
}
