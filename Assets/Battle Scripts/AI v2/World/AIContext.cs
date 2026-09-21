using System.Collections.Generic;

namespace ArmySystem.AI.V2
{
    public sealed class AIContext
    {
        public Battle Battle { get; set; }
        public ArmySnapshot FriendlyArmy { get; set; }
        public ArmySnapshot EnemyArmy { get; set; }
        public IReadOnlyList<ObjectiveSnapshot> Objectives { get; set; }
        public IWorldView World { get; set; }
        public float BattleTime { get; set; }
        public long Version { get; set; }
    }

    public interface IWorldView
    {
        float InfluenceAt(UnityEngine.Vector2 position, InfluenceChannel channel);
        bool HasLineOfSight(UnityEngine.Vector2 from, UnityEngine.Vector2 to);
        bool IsTraversable(UnityEngine.Vector2 position, IAISquad squad);
    }
}
