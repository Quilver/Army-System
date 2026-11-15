using System.Collections.Generic;
using UnityEngine;

// Assault (spec §4.2, SU-165): a troop with an active order (Move, Pursue or Line) that becomes engaged opens an Assault.
// While it is open, every soldier that may charge (ChargeCoordinator's eligibility: front rank, charge gap, a target in
// reach) and has not yet made its Assault charge charges at once, with no dice roll: each soldier once per Assault, as
// it comes into reach. It closes when the troop is no longer engaged (with all its soldiers back in the formation, so
// its own chargers leaving the footprint do not end it), retreats (or slips with a retreating chain), is disordered or
// routs, or when every living soldier has made its charge (Troop.RefreshAssault). A later engagement with an active order opens a
// fresh one. A troop without an order never assaults.
// Chain reaction (SU-171, Melee Tuning Assault Chain Reaction): the first soldier in reach (the spark) charges at once.
// The rest wait until the troop has stopped wheeling (the assault wheel, SU-163) for AssaultChainSettleSeconds, or
// AssaultChainMaxWaitSeconds after the spark, whichever is first. Then the reaction spreads along the line from where the
// spark charged at AssaultChainSpeed: a soldier may make its Assault charge once the reaction has reached its file (its
// distance from the spark along the troop's line). A soldier the reaction has passed charges as soon as it has a target
// in reach, as before.
// One wave (SU-174, Melee Tuning Assault One Wave): the spark does not charge; it only starts the same wait. Once the
// reaction is released every soldier that may make its Assault charge charges at once (the spread is not used).
public sealed class AssaultState
{
    readonly HashSet<SoldierAgentState> _charged = new();
    bool _open;
    bool _sparked;
    Vector2 _sparkPoint;
    float _sinceSpark;
    float _settled;
    bool _released;
    float _sinceRelease;

    public bool IsActive => _open;
    // Soldiers that have made their Assault charge in the open (or last) Assault.
    public int ChargesMade => _charged.Count;

    // Called on the frame the troop becomes engaged. An Assault still open carries on.
    public void OnEngaged(bool hasActiveOrder)
    {
        if (!hasActiveOrder || _open) return;
        _open = true;
        _charged.Clear();
        _sparked = false;
        _released = false;
        _sinceSpark = 0f;
        _settled = 0f;
        _sinceRelease = 0f;
    }

    // The spark has charged (SU-171).
    public bool HasSpark => _open && _sparked;
    // The reaction has started spreading from the spark.
    public bool IsReleased => _open && _released;

    // Records the spark: the first Assault charge, made where point is.
    public void Spark(Vector2 point)
    {
        if (!_open || _sparked) return;
        _sparked = true;
        _sparkPoint = point;
        _sinceSpark = 0f;
        _settled = 0f;
    }

    // Advances the reaction by deltaTime: until it is released, it waits for the troop to have stopped wheeling for
    // settleSeconds, or for maxWaitSeconds after the spark.
    public void Tick(float deltaTime, bool wheeling, float settleSeconds, float maxWaitSeconds)
    {
        if (!_open || !_sparked) return;
        deltaTime = Mathf.Max(0f, deltaTime);
        if (_released)
        {
            _sinceRelease += deltaTime;
            return;
        }
        _sinceSpark += deltaTime;
        _settled = wheeling ? 0f : _settled + deltaTime;
        if (_settled >= settleSeconds || _sinceSpark >= maxWaitSeconds)
        {
            _released = true;
            _sinceRelease = 0f;
        }
    }

    // Whether the reaction has reached a soldier standing at position: its distance from the spark along lineAxis is
    // within how far the reaction has spread (speed x the time since it was released).
    public bool HasReached(Vector2 position, Vector2 lineAxis, float speed)
    {
        if (!_open || !_sparked || !_released) return false;
        float along = Mathf.Abs(Vector2.Dot(position - _sparkPoint, lineAxis.normalized));
        return along <= Mathf.Max(0f, speed) * _sinceRelease + 1e-4f;
    }

    public bool Owes(SoldierAgentState soldier) => _open && soldier != null && !_charged.Contains(soldier);

    public void MarkCharged(SoldierAgentState soldier)
    {
        if (_open && soldier != null) _charged.Add(soldier);
    }

    // Closes the Assault once every living soldier has made its charge.
    public void CloseIfComplete(IReadOnlyList<SoldierAgentState> soldiers)
    {
        if (!_open) return;
        foreach (var soldier in soldiers)
            if (soldier.IsAlive && !_charged.Contains(soldier)) return;
        _open = false;
    }

    public void Clear()
    {
        _open = false;
        _sparked = false;
        _released = false;
    }
}
