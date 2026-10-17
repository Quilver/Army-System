using System.Collections.Generic;
using UnityEngine;

// A player order to stand in a battle line: turns the troop to face the line, and marches to the line centre. While
// the troop is stationary after the order, its soldiers fill a line along its local Y axis (see SoldierFieldPlanner).
// The order stays active until the troop arrives or is overridden.
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

    // True from the moment a line order is given until the troop arrives: the soldiers hold a line shape while idle.
    public bool HasIdleLine { get; private set; }

    public void Cancel() => IsActive = false;

    // Reached the destination: soldiers relax back into a blob and an ordered (non-threat) formation is dropped.
    public void CompleteOnArrival()
    {
        if (!IsActive) return;
        IsActive = false;
        HasIdleLine = false;
        _troop.FormationState.ClearOrderedFormation();
    }

    public void Order(Vector2 center, Vector2 tangent, float width, Vector2 enemyHint)
    {
        if (tangent.sqrMagnitude < 0.001f)
            tangent = _troop.LineAxis;
        tangent.Normalize();

        _troop.LineWidth = Mathf.Clamp(width, _troop.MinimumLineWidth, _troop.Radius * 1.8f);
        IsActive = true;
        HasIdleLine = true;

        Vector2 forward = new(tangent.y, -tangent.x);
        Vector2 towardEnemy = enemyHint - center;
        if (towardEnemy.sqrMagnitude > 0.001f && Vector2.Dot(forward, towardEnemy) < 0f)
            forward = -forward;

        RotateKeepingSoldiersInPlace(forward);
        _troop.MoveTo(center);
        IsActive = true;
    }

    // Rotates the troop transform without moving any soldier in world space.
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