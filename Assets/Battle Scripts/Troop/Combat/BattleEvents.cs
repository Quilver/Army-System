using System;
using UnityEngine;

// Battle-wide notifications for code outside the troop simulation: battle end conditions, achievements, the battle
// report and sound. The simulation raises them; it never reads them. Cleared when play mode starts, so handlers from a
// previous session never fire.
public static class BattleEvents
{
    // A troop is out of the battle: it routed, or its last soldier died before it could rout. Raised once per troop.
    public static event Action<Troop> TroopDefeated;

    // A soldier died. attacker is the troop that dealt the killing blow, or null when unknown.
    public static event Action<Troop, SoldierAgentState, Troop> SoldierKilled;

    // A melee hit that dealt damage: attacker, victim, and the victim soldier's world position.
    public static event Action<Troop, Troop, Vector2> SoldierStruck;

    // A troop fired a volley.
    public static event Action<Troop> VolleyFired;

    // A projectile reached its target soldier: shooter (may be null), target troop, hit or miss, world position.
    public static event Action<Troop, Troop, bool, Vector2> ProjectileLanded;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Clear()
    {
        TroopDefeated = null;
        SoldierKilled = null;
        SoldierStruck = null;
        VolleyFired = null;
        ProjectileLanded = null;
    }

    internal static void RaiseTroopDefeated(Troop troop) => TroopDefeated?.Invoke(troop);
    internal static void RaiseSoldierKilled(Troop victim, SoldierAgentState soldier, Troop attacker) =>
        SoldierKilled?.Invoke(victim, soldier, attacker);
    internal static void RaiseSoldierStruck(Troop attacker, Troop victim, Vector2 position) =>
        SoldierStruck?.Invoke(attacker, victim, position);
    internal static void RaiseVolleyFired(Troop shooter) => VolleyFired?.Invoke(shooter);
    internal static void RaiseProjectileLanded(Troop shooter, Troop target, bool hit, Vector2 position) =>
        ProjectileLanded?.Invoke(shooter, target, hit, position);
}
