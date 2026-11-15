using System.Collections.Generic;

// Assault (spec §4.2, SU-165): a troop with an active order (Move, Pursue or Line) that becomes engaged opens an Assault.
// While it is open, every soldier that may charge (ChargeCoordinator's eligibility: front rank, charge gap, a target in
// reach) and has not yet made its Assault charge charges at once, with no dice roll: each soldier once per Assault, as
// it comes into reach. It closes when the troop is no longer engaged (with all its soldiers back in the formation, so
// its own chargers leaving the footprint do not end it), retreats (or slips with a retreating chain), is disordered or
// routs, or when every living soldier has made its charge (Troop.RefreshAssault). A later engagement with an active order opens a
// fresh one. A troop without an order never assaults.
public sealed class AssaultState
{
    readonly HashSet<SoldierAgentState> _charged = new();
    bool _open;

    public bool IsActive => _open;
    // Soldiers that have made their Assault charge in the open (or last) Assault.
    public int ChargesMade => _charged.Count;

    // Called on the frame the troop becomes engaged. An Assault still open carries on.
    public void OnEngaged(bool hasActiveOrder)
    {
        if (!hasActiveOrder || _open) return;
        _open = true;
        _charged.Clear();
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

    public void Clear() => _open = false;
}
