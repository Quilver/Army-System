using System.Collections.Generic;
using UnityEngine;

namespace LeveragedWars.Battle.AI
{
    /// <summary>
    /// Shared facts every AI layer reads. Refreshed from the battlefield before each tick,
    /// so the AI can also be driven from recorded snapshots in tests.
    /// </summary>
    public sealed class AIBlackboard
    {
        readonly List<IAITroop> aiTroops = new List<IAITroop>();
        readonly List<IAITroop> opponents = new List<IAITroop>();
        readonly Dictionary<string, IAIObjective> objectives = new Dictionary<string, IAIObjective>();
        readonly Dictionary<IAITroop, AITroopAgent> agents = new Dictionary<IAITroop, AITroopAgent>();
        readonly Dictionary<string, Detachment> detachments = new Dictionary<string, Detachment>();
        readonly List<Detachment> detachmentOrder = new List<Detachment>();
        readonly HashSet<string> flags = new HashSet<string>();
        readonly Dictionary<string, float> triggerFireTimes = new Dictionary<string, float>();

        public AIBlackboard(AITeam aiTeam, float time)
        {
            AiTeam = aiTeam;
            Time = time;
            BattleStartTime = time;
        }

        public AITeam AiTeam { get; }
        public float Time { get; private set; }
        public float BattleStartTime { get; }
        public float Elapsed => Time - BattleStartTime;

        /// <summary>AI troops that are alive and not routing.</summary>
        public IReadOnlyList<IAITroop> ActiveAiTroops => aiTroops;
        /// <summary>Opposing troops that are alive and not routing.</summary>
        public IReadOnlyList<IAITroop> ActiveOpponents => opponents;
        public IReadOnlyList<Detachment> Detachments => detachmentOrder;
        public IEnumerable<AITroopAgent> Agents => agents.Values;

        public void Refresh(IAIBattlefield battlefield, float time)
        {
            Time = time;
            aiTroops.Clear();
            opponents.Clear();
            IReadOnlyList<IAITroop> troops = battlefield.Troops;
            for (int i = 0; i < troops.Count; i++)
            {
                IAITroop t = troops[i];
                if (t == null || !t.IsAlive || t.IsRouting) continue;
                if (t.Team == AiTeam) aiTroops.Add(t);
                else if (t.Team != AITeam.None) opponents.Add(t);
            }

            objectives.Clear();
            IReadOnlyList<IAIObjective> list = battlefield.Objectives;
            for (int i = 0; i < list.Count; i++)
                if (list[i] != null) objectives[list[i].Id] = list[i];

            for (int i = 0; i < detachmentOrder.Count; i++) detachmentOrder[i].RefreshActive();
        }

        // ---- Agents and detachments ----

        public AITroopAgent GetOrCreateAgent(IAITroop troop)
        {
            if (!agents.TryGetValue(troop, out AITroopAgent agent))
            {
                agent = new AITroopAgent(troop);
                agents.Add(troop, agent);
            }
            return agent;
        }

        public bool AddDetachment(Detachment detachment)
        {
            if (detachments.ContainsKey(detachment.Id))
            {
                Debug.LogError($"[AI] Duplicate detachment id '{detachment.Id}'.");
                return false;
            }
            detachments.Add(detachment.Id, detachment);
            detachmentOrder.Add(detachment);
            return true;
        }

        public Detachment GetDetachment(string id) =>
            !string.IsNullOrEmpty(id) && detachments.TryGetValue(id, out Detachment d) ? d : null;

        public IAIObjective GetObjective(string id) =>
            !string.IsNullOrEmpty(id) && objectives.TryGetValue(id, out IAIObjective o) ? o : null;

        // ---- Flags and trigger history ----

        public bool HasFlag(string flag) => !string.IsNullOrEmpty(flag) && flags.Contains(flag);

        public void SetFlag(string flag, bool value)
        {
            if (string.IsNullOrEmpty(flag)) return;
            if (value) flags.Add(flag); else flags.Remove(flag);
        }

        public void RecordTriggerFired(string id)
        {
            if (!string.IsNullOrEmpty(id)) triggerFireTimes[id] = Time;
        }

        public bool TryGetTriggerFireTime(string id, out float time) =>
            triggerFireTimes.TryGetValue(id ?? string.Empty, out time);

        // ---- Queries ----

        public IAITroop NearestOpponent(Vector3 from, float maxDistance = float.PositiveInfinity)
        {
            IAITroop best = null;
            float bestSq = maxDistance * maxDistance;
            for (int i = 0; i < opponents.Count; i++)
            {
                float sq = AIGeometry.Flat(opponents[i].Position - from).sqrMagnitude;
                if (sq <= bestSq) { bestSq = sq; best = opponents[i]; }
            }
            return best;
        }

        public bool AnyOpponentWithin(Vector3 point, float radius)
        {
            float r2 = radius * radius;
            for (int i = 0; i < opponents.Count; i++)
                if (AIGeometry.Flat(opponents[i].Position - point).sqrMagnitude <= r2) return true;
            return false;
        }

        public void OpponentsWithin(Vector3 point, float radius, List<IAITroop> results, bool append = false)
        {
            if (!append) results.Clear();
            float r2 = radius * radius;
            for (int i = 0; i < opponents.Count; i++)
                if (AIGeometry.Flat(opponents[i].Position - point).sqrMagnitude <= r2) results.Add(opponents[i]);
        }

        /// <summary>Nearest opponent in range that is not locked in melee (so friendly troops are not hit).</summary>
        public IAITroop BestRangedTarget(IAITroop shooter)
        {
            IAITroop best = null;
            float bestSq = shooter.MaxRange * shooter.MaxRange;
            for (int i = 0; i < opponents.Count; i++)
            {
                IAITroop o = opponents[i];
                if (o.IsInMelee) continue;
                float sq = AIGeometry.Flat(o.Position - shooter.Position).sqrMagnitude;
                if (sq <= bestSq) { bestSq = sq; best = o; }
            }
            return best;
        }

        /// <summary>An opponent within the radius closing on the troop at charge speed, or null.</summary>
        public IAITroop IncomingCharge(IAITroop troop, float radius, float speedFraction)
        {
            for (int i = 0; i < opponents.Count; i++)
            {
                IAITroop o = opponents[i];
                if (o.RunSpeed <= 0f) continue;
                Vector3 toTroop = AIGeometry.Flat(troop.Position - o.Position);
                float distance = toTroop.magnitude;
                if (distance > radius || distance < 0.01f) continue;
                float closing = Vector3.Dot(AIGeometry.Flat(o.Velocity), toTroop / distance);
                if (closing >= o.RunSpeed * speedFraction) return o;
            }
            return null;
        }

        public Vector3 AiCentroid()
        {
            if (aiTroops.Count == 0) return Vector3.zero;
            Vector3 sum = Vector3.zero;
            for (int i = 0; i < aiTroops.Count; i++) sum += aiTroops[i].Position;
            return sum / aiTroops.Count;
        }

        /// <summary>The opponent furthest from the AI army's centre.</summary>
        public IAITroop RearmostOpponent()
        {
            IAITroop best = null;
            float bestSq = -1f;
            Vector3 centre = AiCentroid();
            for (int i = 0; i < opponents.Count; i++)
            {
                float sq = AIGeometry.Flat(opponents[i].Position - centre).sqrMagnitude;
                if (sq > bestSq) { bestSq = sq; best = opponents[i]; }
            }
            return best;
        }

        /// <summary>An opponent currently fighting one of the detachment's troops in melee, or null.</summary>
        public IAITroop OpponentEngagedWith(Detachment detachment)
        {
            if (detachment == null) return null;
            IReadOnlyList<AITroopAgent> active = detachment.ActiveAgents;
            for (int i = 0; i < active.Count; i++)
            {
                IAITroop troop = active[i].Troop;
                IAITroop o = troop.IsInMelee ? troop.MeleeOpponent : null;
                if (o != null && o.IsAlive && !o.IsRouting && o.Team != AiTeam) return o;
            }
            return null;
        }
    }
}
