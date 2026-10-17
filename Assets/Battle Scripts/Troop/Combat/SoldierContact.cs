using UnityEngine;

// A living soldier together with its owning troop and world position, as sampled for one combat frame.
public readonly struct SoldierContact
{
    public readonly Troop Troop;
    public readonly SoldierAgentState Soldier;
    public readonly Vector2 Position;

    public SoldierContact(Troop troop, SoldierAgentState soldier, Vector2 position)
    {
        Troop = troop;
        Soldier = soldier;
        Position = position;
    }
}