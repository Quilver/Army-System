using System.Collections.Generic;

namespace ArmySystem.AI.V2
{
    public interface IArmyPlanner
    {
        ArmyPlan CreatePlan(AIContext context, IReadOnlyList<IAISquad> squads);
        bool ShouldReplan(AIContext context, ArmyPlan currentPlan);
    }

    public sealed class ArmyPlan
    {
        public string Id { get; set; }
        public ArmyPosture Posture { get; set; }
        public float Utility { get; set; }
        public IReadOnlyList<SquadAssignment> Assignments { get; set; }
        public IReadOnlyList<PlanningAssumption> Assumptions { get; set; }
    }

    public sealed class SquadAssignment
    {
        public IAISquad Squad { get; set; }
        public SquadTaskDefinition Task { get; set; }
        public IAIObjective Objective { get; set; }
        public PlannedRoute Route { get; set; }
        public int Priority { get; set; }
        public string Reason { get; set; }
    }

    public readonly struct PlanningAssumption
    {
        public PlanningAssumption(string description, bool isSatisfied)
        {
            Description = description;
            IsSatisfied = isSatisfied;
        }

        public string Description { get; }
        public bool IsSatisfied { get; }
    }
}
