using System.Collections.Generic;

namespace ArmySystem.AI.V2
{
    public interface IManeuverPlanner
    {
        ManeuverProposal CreateProposal(ManeuverRequest request);
    }

    public sealed class ManeuverRequest
    {
        public AIContext Context { get; set; }
        public IReadOnlyList<IAISquad> AvailableSquads { get; set; }
        public IAIObjective Objective { get; set; }
        public ForceGroup TargetGroup { get; set; }
    }

    public sealed class ManeuverProposal
    {
        public string Id { get; set; }
        public float Utility { get; set; }
        public bool IsFeasible { get; set; }
        public IReadOnlyList<SquadAssignment> Assignments { get; set; }
        public IReadOnlyList<PlannedRoute> Routes { get; set; }
        public IReadOnlyList<PlanningAssumption> Assumptions { get; set; }
    }

    public interface ISquadAllocator
    {
        IReadOnlyList<IAISquad> Select(
            IReadOnlyList<IAISquad> availableSquads,
            IUnitRequirement requirement,
            float requestedStrength,
            AIContext context);
    }
}
