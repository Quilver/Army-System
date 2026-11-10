using System.Collections.Generic;
using UnityEngine;

namespace LeveragedWars.Battle.AI
{
    /// <summary>
    /// Everything on the field the AI can see (IAIBattlefield): every Troop and CapturePoint in the scene, through their
    /// adapters. Adds a TroopAIAdapter to each troop and a CapturePointAIObjective to each capture point that lacks one,
    /// at Awake (so detachments can list troops before the battle starts) and again for troops spawned later, such as the
    /// deployed roster. Destroyed troops are dropped from the list.
    /// Put it on the battle manager (the GameObject with Battle) or on the EnemyAI object.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class BattleAIBattlefield : MonoBehaviour, IAIBattlefield
    {
        [SerializeField, Min(0.1f), Tooltip("How often to look for troops and capture points spawned during the battle (seconds).")]
        float rescanInterval = 0.5f;

        [SerializeField, Tooltip("Team id of the player's troops, for troops under neither of the battle's armies.")]
        int playerTeamId = 0;

        readonly List<IAITroop> troops = new List<IAITroop>();
        readonly List<IAIObjective> objectives = new List<IAIObjective>();
        readonly List<TroopAIAdapter> adapters = new List<TroopAIAdapter>();
        readonly List<CapturePointAIObjective> objectiveAdapters = new List<CapturePointAIObjective>();
        float nextScan;
        int prunedFrame = -1;

        public IReadOnlyList<IAITroop> Troops
        {
            get
            {
                Refresh();
                return troops;
            }
        }

        public IReadOnlyList<IAIObjective> Objectives
        {
            get
            {
                Refresh();
                return objectives;
            }
        }

        void Awake()
        {
            TroopAIAdapter.PlayerTeamId = playerTeamId;
            Scan();
        }

        void Refresh()
        {
            if (Time.time >= nextScan) Scan();
            else if (prunedFrame != Time.frameCount) Prune();
        }

        void Scan()
        {
            nextScan = Time.time + rescanInterval;

            adapters.Clear();
            Troop[] found = FindObjectsByType<Troop>(FindObjectsSortMode.None);
            // Sorted so the AI sees troops in the same order every run.
            System.Array.Sort(found, (a, b) => a.GetInstanceID().CompareTo(b.GetInstanceID()));
            foreach (Troop troop in found) adapters.Add(TroopAIAdapter.For(troop));

            objectiveAdapters.Clear();
            foreach (CapturePoint point in FindObjectsByType<CapturePoint>(FindObjectsSortMode.None))
                objectiveAdapters.Add(CapturePointAIObjective.For(point));

            Prune();
        }

        // Rebuilds the interface lists without destroyed objects (a destroyed component is not null through an interface).
        void Prune()
        {
            prunedFrame = Time.frameCount;
            troops.Clear();
            foreach (TroopAIAdapter adapter in adapters)
                if (adapter != null) troops.Add(adapter);
            objectives.Clear();
            foreach (CapturePointAIObjective objective in objectiveAdapters)
                if (objective != null) objectives.Add(objective);
        }
    }
}
