using System.Collections.Generic;
using UnityEngine;

namespace LeveragedWars.Battle.AI
{
    /// <summary>
    /// Runs the enemy AI. Refreshes the blackboard, then ticks the commander and formations
    /// (about 4 times a second) and the troops (about 10 times a second).
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class AIDirector : MonoBehaviour
    {
        [SerializeField, Tooltip("Component implementing IAIBattlefield, e.g. BattleAIBattlefield. Empty = the first one in the scene.")]
        MonoBehaviour battlefield;

        [SerializeField, Tooltip("Component implementing IAICommander, e.g. AIMission. Empty = the one on this GameObject.")]
        MonoBehaviour commander;

        [SerializeField] AITeam aiTeam = AITeam.Enemy;

        [SerializeField, Tooltip("Start when the scene starts. Untick and call BeginBattle() yourself if there is a deployment phase.")]
        bool beginOnStart = true;

        [SerializeField, Min(1f)] float troopTicksPerSecond = 10f;
        [SerializeField, Min(0.5f)] float formationTicksPerSecond = 4f;
        [SerializeField] TroopReflexSettings reflexes = new TroopReflexSettings();

        IAIBattlefield source;
        IAICommander brain;
        float troopTimer;
        float formationTimer;

        public AIBlackboard Blackboard { get; private set; }
        public bool IsRunning => Blackboard != null;

        void Start()
        {
            if (beginOnStart) BeginBattle();
        }

        public void BeginBattle()
        {
            if (IsRunning) return;
            source = battlefield as IAIBattlefield ?? FindInScene<IAIBattlefield>();
            brain = commander as IAICommander ?? GetComponent<IAICommander>();
            if (source == null || brain == null)
            {
                Debug.LogError("[AI] AIDirector needs a battlefield (IAIBattlefield) and a commander (IAICommander).", this);
                return;
            }

            Blackboard = new AIBlackboard(aiTeam, Time.time);
            Blackboard.Refresh(source, Time.time);
            brain.Initialise(Blackboard);
            TickFormations();
            TickTroops();
        }

        public void EndBattle() => Blackboard = null;

        static T FindInScene<T>() where T : class
        {
            foreach (MonoBehaviour behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                if (behaviour is T found) return found;
            return null;
        }

        void Update()
        {
            if (!IsRunning) return;

            float dt = Time.deltaTime;
            troopTimer += dt;
            formationTimer += dt;
            float formationInterval = 1f / formationTicksPerSecond;
            float troopInterval = 1f / troopTicksPerSecond;
            bool formations = formationTimer >= formationInterval;
            bool troops = troopTimer >= troopInterval;
            if (!formations && !troops) return;

            Blackboard.Refresh(source, Time.time);
            if (formations)
            {
                formationTimer %= formationInterval;
                TickFormations();
            }
            if (troops)
            {
                troopTimer %= troopInterval;
                TickTroops();
            }
        }

        void TickFormations()
        {
            brain.Tick(Blackboard);
            IReadOnlyList<Detachment> detachments = Blackboard.Detachments;
            for (int i = 0; i < detachments.Count; i++) detachments[i].Tick();
        }

        void TickTroops()
        {
            foreach (AITroopAgent agent in Blackboard.Agents) agent.Tick(Blackboard, reflexes);
        }

        void OnValidate()
        {
            if (battlefield != null && !(battlefield is IAIBattlefield))
            {
                Debug.LogWarning($"[AI] {battlefield.GetType().Name} does not implement IAIBattlefield.", this);
                battlefield = null;
            }
            if (commander != null && !(commander is IAICommander))
            {
                Debug.LogWarning($"[AI] {commander.GetType().Name} does not implement IAICommander.", this);
                commander = null;
            }
        }
    }
}
