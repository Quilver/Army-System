using UnityEngine;

// Links a soldier's sprite object back to its troop and soldier state, so the Inspector can show that soldier's live
// HP and state (SoldierViewEditor). Added to each soldier object by TroopSoldierViews at runtime.
[DisallowMultipleComponent]
public sealed class SoldierView : MonoBehaviour
{
    public Troop Troop { get; private set; }
    public SoldierAgentState Soldier { get; private set; }

    internal void Bind(Troop troop, SoldierAgentState soldier)
    {
        Troop = troop;
        Soldier = soldier;
    }
}
