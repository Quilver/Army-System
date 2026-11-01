// Formation states in ascending priority (spec §3.1). A higher state overrides a lower one.
public enum FormationMode
{
    Idle,    // blob: stationary, nothing else applies
    Column,  // marching under a Move or Pursue order
    Line,    // an enemy within S_Distance, or holding a player line order
    Chain,   // in Line and linked to a friendly neighbour
    Rout     // broke below the rout threshold; final
}
