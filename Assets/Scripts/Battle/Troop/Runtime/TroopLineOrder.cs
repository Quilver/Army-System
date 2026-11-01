using System.Collections.Generic;
using UnityEngine;

// A player order to stand in a battle line (spec §3.3 trigger 2, §6.2): the troop turns to the ordered facing, marches its
// pivot to the line centre, and holds the line while stationary (HasIdleLine). The order lasts until the player gives
// the troop another order, or the troop breaks.
public sealed class TroopLineOrder
{
    readonly Troop _troop;
    readonly List<Vector2> _worldPositions = new();
    readonly List<Vector2> _worldVelocities = new();

    public TroopLineOrder(Troop troop)
    {
        _troop = troop;
    }

    public bool IsActive { get; private set; }

    // The troop holds the ordered line whenever it is stationary under the order.
    public bool HasIdleLine => IsActive;

    public Vector2 Center { get; private set; }
    public Vector2 Facing { get; private set; } = Vector2.right;
    public float Length { get; private set; }

    public void Cancel() => IsActive = false;

    // facing is the ordered front direction; length is already clamped by the caller.
    public void Order(Vector2 center, Vector2 facing, float length)
    {
        if (facing.sqrMagnitude < 0.001f) facing = _troop.transform.right;
        facing.Normalize();

        Center = center;
        Facing = facing;
        Length = Mathf.Max(0.1f, length);
        IsActive = true;

        RotateKeepingSoldiersInPlace(facing);
    }

    // Rotates the troop transform so its local X axis is the facing, without moving any soldier in world space.
    void RotateKeepingSoldiersInPlace(Vector2 forward)
    {
        Transform transform = _troop.transform;
        List<SoldierAgentState> soldiers = _troop.Roster.SoldierList;
        _worldPositions.Clear();
        _worldVelocities.Clear();
        foreach (var soldier in soldiers)
        {
            _worldPositions.Add(transform.TransformPoint(soldier.Position));
            _worldVelocities.Add(transform.TransformVector(soldier.Velocity));
        }
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg);
        for (int i = 0; i < soldiers.Count; i++)
        {
            soldiers[i].Position = transform.InverseTransformPoint(_worldPositions[i]);
            soldiers[i].Velocity = transform.InverseTransformVector(_worldVelocities[i]);
        }
    }
}
