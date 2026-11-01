using System;
using System.Collections.Generic;
using UnityEngine;

// Controller for battle-wide troop behaviour. It owns no combat rules itself; it only runs each system in order.
// Distances (S_Distance, A_Distance) and every other tuning value come from TroopTuning, so scenes cannot drift from
// the spec values.
//   Update:      footprints and engagement -> chains -> line pressure -> contact index -> separation -> charges ->
//                ranged fire -> projectiles
//   LateUpdate:  contact index on post-move positions -> contacts, bounces and clashes
[DefaultExecutionOrder(-100)]
public sealed class TroopCombatWorld : MonoBehaviour
{
    [SerializeField, Tooltip("Seed for every random draw in this battle. Set a non-zero value to replay a battle exactly; 0 picks a new seed at start.")]
    int _battleSeed;

    readonly TroopRegistry _registry = new();
    readonly SoldierContactIndex _contactIndex = new();
    readonly TroopSegmentPressureRules _segmentPressure = new();
    readonly TroopEngagementSystem _engagement = new();
    readonly TroopProjectiles _projectiles = new();
    readonly ProjectileView _projectileView = new();
    ChargeCoordinator _charges;
    SoldierContactResolver _contactResolver;

    // The live battle controller. Troops use it to reach their seeded random stream and the troop list.
    public static TroopCombatWorld Current { get; private set; }

    public int Seed { get; private set; }
    public IReadOnlyList<Troop> Troops => _registry.Troops;
    public TroopProjectiles Projectiles => _projectiles;

    // Seeded stream for one troop. streamId must be stable across runs (see Troop.StreamId).
    public ITroopRandom RandomFor(int streamId) =>
        new SeededTroopRandom(SeededTroopRandom.DeriveSeed(Seed, streamId));

    // Sets the seed before troops draw from it (tests and replays).
    public void SetSeed(int seed)
    {
        _battleSeed = seed;
        Seed = seed != 0 ? seed : Environment.TickCount;
    }

    void Awake() => Initialize();

    // Called from Awake, and by tests that build a battle outside Play mode.
    internal void Initialize()
    {
        Current = this;
        if (_charges != null) return;
        if (Seed == 0) Seed = _battleSeed != 0 ? _battleSeed : Environment.TickCount;
        _charges = new ChargeCoordinator(_contactIndex, _registry);
        _contactResolver = new SoldierContactResolver(_contactIndex, new DuelResolver(new SupportCalculator(_contactIndex)));
    }

    void OnDestroy()
    {
        if (Current == this) Current = null;
        _projectileView.Destroy();
    }

    // Picks up troops created this frame (the registry otherwise refreshes every 0.5 s).
    public void RefreshTroopsNow() => _registry.Refresh(force: true);

    void Update() => StepBeforeTroops(Time.deltaTime);

    void LateUpdate() => StepAfterTroops();

    // One whole frame outside Play mode (tests): the world's Update, every troop's Update, then the world's LateUpdate.
    internal void SimulateFrame(float deltaTime)
    {
        Initialize();
        _registry.Refresh(force: true);
        StepBeforeTroops(deltaTime);
        foreach (var troop in _registry.Troops)
            if (troop != null) troop.Step(deltaTime);
        StepAfterTroops();
        TroopObjects.FlushPending();
    }

    void StepBeforeTroops(float deltaTime)
    {
        _registry.Refresh();
        _registry.RemoveDestroyed();
        IReadOnlyList<Troop> troops = _registry.Troops;
        _engagement.Update(troops, deltaTime);
        ChainLinker.Update(troops, deltaTime);
        _segmentPressure.Update(troops, TroopTuning.Active.ADistance, deltaTime);
        _contactIndex.Rebuild(troops);
        CrossTroopSeparation.Apply(_contactIndex);
        _charges.Update(deltaTime);
        foreach (var troop in troops)
            if (troop != null) troop.RangedFire.Tick(troops, _projectiles, deltaTime);
        _projectiles.Tick(deltaTime);
        _projectileView.Draw(_projectiles.Active, transform);
    }

    void StepAfterTroops()
    {
        _registry.Refresh();
        _registry.RemoveDestroyed();
        _contactIndex.Rebuild(_registry.Troops);
        _contactResolver.Resolve(_registry.Troops);
    }

    void OnDrawGizmos()
    {
        IReadOnlyList<Troop> troops = Application.isPlaying
            ? _registry.Troops
            : FindObjectsByType<Troop>(FindObjectsSortMode.None);
        TroopCombatGizmos.Draw(troops, TroopTuning.Active.SDistance);
    }
}
