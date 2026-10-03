using System.Collections.Generic;
using UnityEngine;

namespace ArmySystem.AI.V2
{
    public sealed class UnitSnapshot
    {
        public IUnit Unit { get; set; }
        public AIUnitProfile Profile { get; set; }
        public Vector2 Position { get; set; }
        public Vector2 Facing { get; set; }
        public UnitState State { get; set; }
        public UnitCapabilityScore Capabilities { get; set; }
        public float Strength { get; set; }
        public bool IsAlive { get; set; }
        public bool IsInMelee { get; set; }
    }

    public sealed class ArmySnapshot
    {
        public Army Army { get; set; }
        public IReadOnlyList<UnitSnapshot> Units { get; set; }
        public IReadOnlyList<ForceGroup> Groups { get; set; }
        public Vector2 Center { get; set; }
        public Vector2 AverageFacing { get; set; }
        public float TotalStrength { get; set; }
        public float MeleeStrength { get; set; }
        public float RangedStrength { get; set; }
        public float MobileStrength { get; set; }
        public float CasualtyRatio { get; set; }
    }

    public sealed class ObjectiveSnapshot
    {
        public IAIObjective Objective { get; set; }
        public Vector2 Position { get; set; }
        public ObjectiveControl Control { get; set; }
        public float CaptureProgress { get; set; }
        public float StrategicValue { get; set; }
    }

    public interface IWorldSnapshotBuilder
    {
        AIContext Build(Army friendlyArmy, Army enemyArmy, float battleTime);
    }
}
