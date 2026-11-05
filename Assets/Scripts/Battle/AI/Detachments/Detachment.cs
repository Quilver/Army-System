using System.Collections.Generic;
using UnityEngine;

namespace LeveragedWars.Battle.AI
{
    public enum DetachmentState { Waiting, Moving, Engaged, Returning, Done, Broken }

    /// <summary>A grouping of troops the AI moves and fights together. Its formation is its current role.</summary>
    public sealed class Detachment
    {
        readonly List<AITroopAgent> agents = new List<AITroopAgent>();
        readonly List<AITroopAgent> active = new List<AITroopAgent>();

        public Detachment(string id, AITeam team, float brokenThreshold)
        {
            Id = id;
            Team = team;
            BrokenThreshold = Mathf.Clamp01(brokenThreshold);
        }

        public string Id { get; }
        public AITeam Team { get; }
        public float BrokenThreshold { get; }
        public Formation Formation { get; private set; }
        public IReadOnlyList<AITroopAgent> Agents => agents;
        /// <summary>Troops that are alive and not routing.</summary>
        public IReadOnlyList<AITroopAgent> ActiveAgents => active;
        public Vector3 Centre { get; private set; }
        public Vector3 Facing { get; private set; } = Vector3.right;
        public Vector3 StartPosition { get; private set; }
        public Vector3 StartFacing { get; private set; } = Vector3.right;

        /// <summary>Share of troops still fighting (not routed or destroyed).</summary>
        public float Cohesion => agents.Count == 0 ? 0f : (float)active.Count / agents.Count;

        public bool IsBroken => active.Count == 0 || Cohesion < BrokenThreshold;

        /// <summary>True when any troop is in melee or a ranged troop is shooting.</summary>
        public bool IsInCombat
        {
            get
            {
                for (int i = 0; i < active.Count; i++)
                {
                    IAITroop t = active[i].Troop;
                    if (t.IsInMelee) return true;
                    if (t.IsRanged && active[i].Issued.Kind == AIOrderKind.Attack) return true;
                }
                return false;
            }
        }

        public DetachmentState State =>
            IsBroken ? DetachmentState.Broken :
            Formation != null ? Formation.State : DetachmentState.Waiting;

        internal void AddAgent(AITroopAgent agent)
        {
            if (agent.Detachment != null && agent.Detachment != this)
            {
                Debug.LogWarning($"[AI] Troop '{agent.Troop.DisplayName}' moved from '{agent.Detachment.Id}' to '{Id}'.");
                agent.Detachment.agents.Remove(agent);
            }
            agent.Detachment = this;
            if (!agents.Contains(agent)) agents.Add(agent);
        }

        internal void CaptureStart()
        {
            RefreshActive();
            StartPosition = Centre;
            StartFacing = Facing;
        }

        internal void RefreshActive()
        {
            active.Clear();
            Vector3 sum = Vector3.zero;
            Vector3 facing = Vector3.zero;
            for (int i = 0; i < agents.Count; i++)
            {
                AITroopAgent a = agents[i];
                if (!a.IsActive) continue;
                active.Add(a);
                sum += a.Troop.Position;
                facing += AIGeometry.Flat(a.Troop.Forward);
            }
            if (active.Count == 0) return;
            Centre = sum / active.Count;
            if (facing.sqrMagnitude > 0.0001f) Facing = facing.normalized;
        }

        /// <summary>Gives the detachment a new role. Null leaves its troops holding where they stand.</summary>
        public void SetFormation(FormationDefinition definition, AIBlackboard blackboard)
        {
            Formation?.Exit();
            Formation = definition?.Create();
            if (Formation != null)
            {
                Formation.Enter(this, blackboard);
                return;
            }
            for (int i = 0; i < active.Count; i++) active[i].Hold(active[i].Position, active[i].Troop.Forward);
        }

        internal void Tick()
        {
            if (active.Count == 0) return;
            Formation?.Tick();
        }
    }
}
