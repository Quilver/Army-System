using System;
using System.Collections.Generic;

namespace ArmySystem.AI.V2
{
    public interface ISquadTask
    {
        TaskStatus Status { get; }
        void Enter(SquadTaskContext context);
        void Tick(SquadTaskContext context);
        void Cancel(SquadTaskContext context);
        void Exit(SquadTaskContext context);
    }

    [Serializable]
    public abstract class SquadTaskDefinition
    {
        public abstract ISquadTask CreateTask();
    }

    public sealed class SquadTaskContext
    {
        public IAISquad Squad { get; set; }
        public AIContext World { get; set; }
        public IUnitOrderService Orders { get; set; }
        public ITargetSelector TargetSelector { get; set; }
        public IPositionEvaluator Positions { get; set; }
        public IFormationPlanner Formation { get; set; }
    }

    public abstract class SquadTask : ISquadTask
    {
        public TaskStatus Status { get; protected set; } = TaskStatus.Inactive;

        public abstract void Enter(SquadTaskContext context);
        public abstract void Tick(SquadTaskContext context);

        public virtual void Cancel(SquadTaskContext context)
        {
            Status = TaskStatus.Cancelled;
            Exit(context);
        }

        public virtual void Exit(SquadTaskContext context) { }
    }

    public interface ICompositeSquadTask : ISquadTask
    {
        IReadOnlyList<ISquadTask> Children { get; }
    }
}
