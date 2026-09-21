using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArmySystem.AI.V2
{
    [Serializable]
    public sealed class UnitCapabilityScore
    {
        public float meleePower;
        public float rangedPower;
        public float effectiveRange;
        public float movementSpeed;
        public float durability;
        public float capturePower;
        public bool canSkirmish;
        public bool canCharge;
        public bool canCapture;
    }

    public readonly struct UnitCapabilityContext
    {
        public UnitCapabilityContext(IUnit unit, AIContext world)
        {
            Unit = unit;
            World = world;
        }

        public IUnit Unit { get; }
        public AIContext World { get; }
    }

    public interface IUnitCapability
    {
        string Id { get; }
        void Contribute(UnitCapabilityContext context, UnitCapabilityScore score);
    }

    public interface IUnitRequirement
    {
        float Score(AIUnitProfile unit, AIContext context);
    }

    public sealed class AIUnitProfile : MonoBehaviour
    {
        [SerializeReference, SubclassSelector]
        private List<IUnitCapability> capabilities = new();

        public IReadOnlyList<IUnitCapability> Capabilities => capabilities;

        public UnitCapabilityScore Evaluate(IUnit unit, AIContext context)
        {
            UnitCapabilityScore score = new();
            UnitCapabilityContext capabilityContext = new(unit, context);

            foreach (IUnitCapability capability in capabilities)
                capability?.Contribute(capabilityContext, score);

            return score;
        }
    }

    [Serializable]
    public sealed class InfluenceProfile
    {
        public InfluenceChannel channel;
        public AnimationCurve strengthByDistance = AnimationCurve.Linear(0, 1, 1, 0);
        [Min(0)] public float maximumRange;
        public bool requiresLineOfSight;
    }
}
