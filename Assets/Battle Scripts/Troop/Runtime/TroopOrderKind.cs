// The order a troop is carrying out (Troop.CurrentOrder). Shown as a path with arrows when the troop is hovered.
public enum TroopOrderKind
{
    None,
    Move,     // march to a point (RMB on ground)
    Attack,   // pursue an enemy troop (RMB on an enemy)
    Line      // march to an ordered battle line (RMB drag)
}
