// What a troop is doing, for UI and battle rules. Replaces the legacy UnitState. The values match the old enum so
// serialized troops keep their state.
public enum TroopState
{
    Idle = 1,
    Moving = 2,
    Fleeing = 4,
}
