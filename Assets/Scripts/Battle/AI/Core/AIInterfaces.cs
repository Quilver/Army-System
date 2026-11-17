using System.Collections.Generic;
using UnityEngine;

namespace LeveragedWars.Battle.AI
{
    /// <summary>
    /// The AI's view of a troop. Implement this on an adapter over the troop system so the
    /// AI never depends on battle-mechanics classes directly.
    /// </summary>
    public interface IAITroop
    {
        int Id { get; }
        string DisplayName { get; }
        AITeam Team { get; }

        /// <summary>False once every soldier is dead or the troop has left the field.</summary>
        bool IsAlive { get; }
        /// <summary>True while morale has broken. Routing troops are left to the battle mechanics.</summary>
        bool IsRouting { get; }

        Vector3 Position { get; }
        Vector3 Forward { get; }
        /// <summary>Current velocity. Used to spot incoming charges.</summary>
        Vector3 Velocity { get; }
        /// <summary>Width of the troop's front in units. Used to space lines.</summary>
        float Frontage { get; }
        float RunSpeed { get; }

        bool IsRanged { get; }
        /// <summary>Maximum shooting range in units. 0 for melee troops.</summary>
        float MaxRange { get; }
        bool HasAmmo { get; }

        bool IsInMelee { get; }
        /// <summary>The troop this one is fighting in melee (its IAITroop wrapper), or null.</summary>
        IAITroop MeleeOpponent { get; }

        void OrderHold(Vector3 position, Vector3 facing);
        void OrderMove(Vector3 position, Vector3 facing, bool run);
        void OrderAttack(IAITroop target);
        void OrderCharge(IAITroop target);
        void OrderFallBack(Vector3 position);
    }

    /// <summary>
    /// Optional, on the same object as an IAITroop: a troop that can stand in a chain of linked battle lines (spec §3.4).
    /// Formations that build chains (Chain Line) look for it with <c>troop as IAIChainTroop</c>. A chain runs left to
    /// right: each troop's right wing joins its right-hand neighbour's left wing.
    /// </summary>
    public interface IAIChainTroop
    {
        /// <summary>Length of the troop's battle line at its natural width (units).</summary>
        float NaturalLineLength { get; }

        /// <summary>True while the troop holds, or marches to, a line order (an order link needs one on both troops).</summary>
        bool HasLineOrder { get; }

        /// <summary>
        /// The link from this troop's right wing to the other troop's left wing: Linked (in force), Pending (made by an
        /// order, waiting for both troops to stand in place) or None.
        /// </summary>
        AIChainLink RightLinkTo(IAIChainTroop other);

        /// <summary>
        /// Joins this troop's right wing to the other's left wing, as a multi-troop line order does: the link is pending
        /// until both troops stand in their places, and lasts while both keep a line order. Replaces any link on either
        /// wing. Returns false if it cannot be made (no line order, other team, or it would close a loop).
        /// </summary>
        bool LinkRightTo(IAIChainTroop other);
    }

    public enum AIChainLink { None, Pending, Linked }

    /// <summary>A capturable objective.</summary>
    public interface IAIObjective
    {
        /// <summary>Id used by formations and triggers, e.g. "Primary".</summary>
        string Id { get; }
        Vector3 Position { get; }
        float Radius { get; }
        AITeam Owner { get; }
        bool IsContested { get; }
    }

    /// <summary>Everything on the field the AI can see. Usually implemented by the battle manager.</summary>
    public interface IAIBattlefield
    {
        /// <summary>All troops. Dead troops may stay in the list with IsAlive = false.</summary>
        IReadOnlyList<IAITroop> Troops { get; }
        IReadOnlyList<IAIObjective> Objectives { get; }
    }

    /// <summary>
    /// Top layer: chooses formations for detachments. AIMission is the Basic tier;
    /// the Advanced tier's Army Planner will implement this too.
    /// </summary>
    public interface IAICommander
    {
        void Initialise(AIBlackboard blackboard);
        void Tick(AIBlackboard blackboard);
    }
}
