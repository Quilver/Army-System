// Tuning values shared by more than one troop system.
public static class TroopConstants
{
    // Neighbour search radius for soldier steering, also used as the cell size of the cross-troop contact grid.
    public const float NeighborRadius = 2.5f;

    // Collision radius of a single soldier.
    public const float SoldierRadius = 0.3f;

    // Soldiers of the same troop keep their centres at least this many soldier radii apart. Charging soldiers are the
    // exception: they slip through their own flock to reach an enemy.
    public const float SeparationRadiusScale = 3f;

    // Spacing between soldiers standing in a battle line.
    public const float LineSpacing = 0.6f;

    // Speed multiplier for a soldier charging an enemy (applies to max speed and acceleration).
    public const float ChargeSpeedScale = 2f;

    // How long in seconds an attacking soldier lunges at its opponent at full (not doubled) speed.
    public const float LungeDuration = 1.5f;

    // Window in seconds over which SoldierProfile.Aggression is counted: Aggression is the expected number of melee
    // attacks a soldier makes per PerMeleeTime seconds of engagement.
    public const float PerMeleeTime = 30f;

    // Upper bound of SoldierProfile.Aggression. Aggression / MaxAggression is used as a 0-1 fraction in other rules.
    public const float MaxAggression = 20f;

    // Interval in seconds between a troop's shared strike beats. Soldiers with Discipline hold some attacks for a beat.
    public const float MeleeBeatInterval = 1f;

    // Upper bound of SoldierProfile.Discipline. Discipline / MaxDiscipline is the chance an attack waits for the beat.
    public const float MaxDiscipline = 20f;
}