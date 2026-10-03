using UnityEngine;

namespace ArmySystem.AI.V2
{
    public interface IAIObjective
    {
        string Id { get; }
        Vector2 Position { get; }
        float StrategicValue { get; }
        ObjectiveSnapshot Observe(Army observingArmy);
    }
}
