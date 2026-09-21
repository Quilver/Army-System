using System;
using UnityEngine;

namespace ArmySystem.AI.V2
{
    public interface IAITrigger
    {
        bool IsSatisfied(AIContext context, IAISquad squad);
    }

    public interface ITaskCompletionCondition
    {
        bool IsSatisfied(SquadTaskContext context);
    }

    [Serializable]
    public sealed class TaskTransition
    {
        [SerializeReference, SubclassSelector] public IAITrigger trigger;
        [SerializeReference, SubclassSelector] public SquadTaskDefinition nextTask;
        public int priority;
        public bool evaluateOnce;
    }
}
