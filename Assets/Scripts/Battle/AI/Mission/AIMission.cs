using System;
using System.Collections.Generic;
using UnityEngine;

namespace LeveragedWars.Battle.AI
{
    [Serializable]
    public sealed class DetachmentSetup
    {
        public string id;

        [Tooltip("Troops in this detachment: components (or GameObjects with a component) implementing IAITroop.")]
        public List<Component> troops = new List<Component>();

        [UnityEngine.Range(0f, 1f), Tooltip("Counts as broken when the share of troops still fighting drops below this.")]
        public float brokenThreshold = 0.5f;

        [SerializeReference, SubclassSelector] public FormationDefinition startingFormation;
    }

    /// <summary>
    /// Basic-tier commander for one mission: authored detachments, their starting formations,
    /// and the triggers that change them during the battle.
    /// </summary>
    public sealed class AIMission : MonoBehaviour, IAICommander
    {
        sealed class TriggerRuntime
        {
            public TriggerDefinition Definition;
            public bool Enabled;
            public bool WasTrue;
        }

        [SerializeField] List<DetachmentSetup> detachments = new List<DetachmentSetup>();
        [SerializeField] List<TriggerDefinition> triggers = new List<TriggerDefinition>();
        [SerializeField, Tooltip("Log each trigger to the console as it fires.")] bool logTriggers = true;

        readonly List<TriggerRuntime> runtime = new List<TriggerRuntime>();

        public IReadOnlyList<DetachmentSetup> Detachments => detachments;
        public IReadOnlyList<TriggerDefinition> Triggers => triggers;

        /// <summary>Replaces the authored detachments and triggers. Used by mission setup tools; takes effect at the next Initialise.</summary>
        public void Configure(IEnumerable<DetachmentSetup> newDetachments, IEnumerable<TriggerDefinition> newTriggers)
        {
            detachments = new List<DetachmentSetup>(newDetachments);
            triggers = new List<TriggerDefinition>(newTriggers);
        }

        public void Initialise(AIBlackboard blackboard)
        {
            runtime.Clear();
            var created = new List<(Detachment detachment, DetachmentSetup setup)>();

            foreach (DetachmentSetup setup in detachments)
            {
                if (string.IsNullOrWhiteSpace(setup.id))
                {
                    Debug.LogError("[AI] A detachment has no id.", this);
                    continue;
                }

                var detachment = new Detachment(setup.id, blackboard.AiTeam, setup.brokenThreshold);
                foreach (Component c in setup.troops)
                {
                    IAITroop troop = c as IAITroop ?? (c != null ? c.GetComponent<IAITroop>() : null);
                    if (troop == null)
                    {
                        Debug.LogError($"[AI] '{(c != null ? c.name : "empty slot")}' in detachment '{setup.id}' has no IAITroop.", this);
                        continue;
                    }
                    if (troop.Team != blackboard.AiTeam)
                        Debug.LogWarning($"[AI] Troop '{troop.DisplayName}' in '{setup.id}' is not on the AI team.", c);
                    detachment.AddAgent(blackboard.GetOrCreateAgent(troop));
                }

                if (blackboard.AddDetachment(detachment)) created.Add((detachment, setup));
            }

            // Record every start position first so anchors to other detachments resolve.
            foreach (var entry in created) entry.detachment.CaptureStart();
            foreach (var entry in created) entry.detachment.SetFormation(entry.setup.startingFormation, blackboard);

            foreach (TriggerDefinition t in triggers)
                if (t != null) runtime.Add(new TriggerRuntime { Definition = t, Enabled = t.enabledAtStart });
        }

        public void Tick(AIBlackboard blackboard)
        {
            for (int i = 0; i < runtime.Count; i++)
            {
                TriggerRuntime t = runtime[i];
                if (!t.Enabled) continue;
                bool isTrue = t.Definition.condition != null && t.Definition.condition.Evaluate(blackboard);
                if (isTrue && !t.WasTrue) Fire(t, blackboard);
                t.WasTrue = isTrue;
            }
        }

        /// <summary>Enables or disables a trigger. A re-enabled trigger fires straight away if its condition already holds.</summary>
        public void SetTriggerEnabled(string id, bool enabled)
        {
            foreach (TriggerRuntime t in runtime)
            {
                if (t.Definition.id != id) continue;
                t.Enabled = enabled;
                t.WasTrue = false;
                return;
            }
            Debug.LogWarning($"[AI] No trigger '{id}' to {(enabled ? "enable" : "disable")}.", this);
        }

        void Fire(TriggerRuntime t, AIBlackboard blackboard)
        {
            if (logTriggers) Debug.Log($"[AI] Trigger '{t.Definition.id}' fired at {blackboard.Elapsed:0.0}s.", this);
            blackboard.RecordTriggerFired(t.Definition.id);
            if (t.Definition.fireOnce) t.Enabled = false;
            foreach (TriggerAction action in t.Definition.actions) action?.Execute(blackboard, this);
        }

        void OnDrawGizmosSelected()
        {
            foreach (TriggerDefinition t in triggers) t?.condition?.DrawGizmos();
        }
    }
}
