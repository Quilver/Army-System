namespace BattlePrep.Tent
{
    // What clicking a tent object does (spec TentHub.md §2).
    public enum TentAction
    {
        None,
        Debts,
        Ledger,
        Map,
        Inventory,  // Weapon Rack (stub)
        Shop,       // Merchant (stub)
        Court,      // talk to court member CourtIndex
        Launch,     // Tent Flaps
    }
}
