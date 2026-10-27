// Fixed values shared by more than one troop system (spec §7.2). Values a designer may tune are on TroopTuning instead.
public static class TroopConstants
{
    // Neighbour search radius for soldier steering, also used as the cell size of the cross-troop contact grid.
    public const float NeighborRadius = 2.5f;

    // Collision radius of a single soldier.
    public const float SoldierRadius = 0.3f;

    // Soldiers of the same troop keep their centres at least SeparationRadiusScale x SoldierRadius (0.9) apart. Charging
    // soldiers and their followers are the exception: they slip through their own troop.
    public const float SeparationRadiusScale = 3f;
    public const float SeparationRadius = SeparationRadiusScale * SoldierRadius;

    // Spacing between soldiers in a battle line and between ranks (spec §3.3). Equal to the minimum soldier spacing.
    public const float LineSpacing = 0.9f;

    // Density spreading radius (spec §2.1).
    public const float SpreadRadius = 1.2f;

    // Extra pressure mass of a fully driving soldier (up to 1 + 3 = 4x a settled one, spec §2.2).
    public const float PressureMassPerDrive = 3f;

    // Opposing soldiers whose centres are closer than this are in contact (spec §4.5).
    public const float ContactDistance = 0.85f;

    // Line points per troop: one per wing (spec §3.4).
    public const int MaxChainLinks = 2;

    // Line point indices. End 0 sits at -tangent, end 1 at +tangent, where the tangent is the facing turned 90 degrees
    // anticlockwise. Facing the enemy, end 0 is on the right wing and end 1 on the left wing.
    public const int RightEnd = 0;
    public const int LeftEnd = 1;
}
