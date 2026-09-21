using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArmySystem.AI.V2
{
    [Serializable]
    public sealed class SquadTuning
    {
        [Min(0)] public float aggroRange = 20;
        [Min(0)] public float disengageRange = 25;
        [Min(0)] public float dangerRange = 8;
        [Min(0)] public float preferredRange = 15;
        [Min(0)] public float formationSpacing = 3;
        [Min(0)] public float regroupDistance = 10;
        [Min(0.05f)] public float decisionInterval = 0.5f;
        [Min(0)] public float targetSwitchThreshold = 2;
    }

    public sealed class AISquadDefinition : MonoBehaviour
    {
        [SerializeField] private string id;
        [SerializeField] private List<IUnit> units = new();
        [SerializeField] private Transform anchor;
        [SerializeField] private Transform rallyPoint;
        [SerializeField] private List<Transform> authoredRoute = new();
        [SerializeField] private SquadTuning tuning = new();
        [SerializeReference, SubclassSelector] private SquadTaskDefinition initialTask;
        [SerializeField] private List<TaskTransition> transitions = new();

        public string Id => id;
        public IReadOnlyList<IUnit> Units => units;
        public Transform Anchor => anchor;
        public Transform RallyPoint => rallyPoint;
        public IReadOnlyList<Transform> AuthoredRoute => authoredRoute;
        public SquadTuning Tuning => tuning;
        public SquadTaskDefinition InitialTask => initialTask;
        public IReadOnlyList<TaskTransition> Transitions => transitions;
    }
}
