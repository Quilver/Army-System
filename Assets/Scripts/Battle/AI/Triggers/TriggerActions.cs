using System;
using System.Collections.Generic;
using UnityEngine;

namespace LeveragedWars.Battle.AI
{
    /// <summary>A condition plus the actions to run when it becomes true.</summary>
    [Serializable]
    public sealed class TriggerDefinition
    {
        public string id;

        public bool enabledAtStart = true;

        [Tooltip("Fire once, then disable. Untick to fire every time the condition becomes true again.")]
        public bool fireOnce = true;

        [SerializeReference, SubclassSelector] public TriggerCondition condition;
        [SerializeReference, SubclassSelector] public List<TriggerAction> actions = new List<TriggerAction>();
    }

    [Serializable]
    public abstract class TriggerAction
    {
        public abstract void Execute(AIBlackboard blackboard, AIMission mission);
    }

    [Serializable, AddTypeMenu("Set Formation")]
    public sealed class SetFormationAction : TriggerAction
    {
        public string detachmentId;
        [SerializeReference, SubclassSelector] public FormationDefinition formation;

        public override void Execute(AIBlackboard blackboard, AIMission mission)
        {
            Detachment d = blackboard.GetDetachment(detachmentId);
            if (d == null)
            {
                Debug.LogWarning($"[AI] Set Formation: no detachment '{detachmentId}'.", mission);
                return;
            }
            d.SetFormation(formation, blackboard);
        }
    }

    [Serializable, AddTypeMenu("Set Flag")]
    public sealed class SetFlagAction : TriggerAction
    {
        public string flag;
        public bool value = true;

        public override void Execute(AIBlackboard blackboard, AIMission mission) => blackboard.SetFlag(flag, value);
    }

    [Serializable, AddTypeMenu("Enable Trigger")]
    public sealed class EnableTriggerAction : TriggerAction
    {
        public string triggerId;
        [Tooltip("Untick to disable the trigger instead.")]
        public bool enabled = true;

        public override void Execute(AIBlackboard blackboard, AIMission mission) => mission.SetTriggerEnabled(triggerId, enabled);
    }
}
