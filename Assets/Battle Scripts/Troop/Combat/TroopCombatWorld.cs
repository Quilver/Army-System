using System.Collections.Generic;
using UnityEngine;

// Controller for battle-wide troop behaviour. It owns no combat rules itself; it only runs each system in order.
[DefaultExecutionOrder(-100)]
public sealed class TroopCombatWorld : MonoBehaviour
{
    [SerializeField, Min(0f)] float _sDistance = 30f;
    [SerializeField, Min(0f)] float _aDistance = 5.2f;

    readonly TroopRegistry _registry = new();
    readonly SoldierContactIndex _contactIndex = new();
    readonly TroopSegmentPressureRules _segmentPressure = new();
    readonly MeleeAttackClock _meleeClock = new();
    ChargeCoordinator _charges;
    SoldierContactResolver _contactResolver;

    void Awake()
    {
        _charges = new ChargeCoordinator(_contactIndex, _registry);
        _contactResolver = new SoldierContactResolver(_contactIndex, new ClashResolver(_contactIndex));
    }

    void Update()
    {
        float deltaTime = Time.deltaTime;
        _registry.Refresh();
        _registry.RemoveDestroyed();
        IReadOnlyList<Troop> troops = _registry.Troops;
        ThreatFormationRules.Update(troops, _sDistance, _aDistance, deltaTime);
        AlliedLineConnector.Connect(troops);
        _segmentPressure.Update(troops, _aDistance, deltaTime);
        _contactIndex.Rebuild(troops);
        CrossTroopSeparation.Apply(_contactIndex);
        _charges.Update(_aDistance, deltaTime);
    }

    void LateUpdate()
    {
        _registry.Refresh();
        _contactIndex.Rebuild(_registry.Troops);
        _contactResolver.Resolve(_registry.Troops, _aDistance);
        _meleeClock.Tick(_registry.Troops, Time.deltaTime);
        _contactResolver.ApplyMeleeStates(_registry.Troops);
    }

    void OnDrawGizmos()
    {
        IReadOnlyList<Troop> troops = Application.isPlaying
            ? _registry.Troops
            : FindObjectsByType<Troop>(FindObjectsSortMode.None);
        TroopCombatGizmos.Draw(troops, _sDistance);
    }
}