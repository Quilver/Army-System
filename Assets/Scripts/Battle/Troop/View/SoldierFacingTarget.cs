// One soldier's facing target while its troop is engaged (SoldierFacingAnimator, SU-158): the enemy soldier it faces and
// how long until it may pick another. Display state only, kept by TroopSoldierViews beside each soldier's sprite.
public sealed class SoldierFacingTarget
{
    float _repickDelay;

    public SoldierAgentState Soldier { get; private set; }
    public Troop Troop { get; private set; }
    public bool CanRepick => _repickDelay <= 0f;

    public void Set(SoldierAgentState soldier, Troop troop)
    {
        Soldier = soldier;
        Troop = troop;
    }

    public void Clear()
    {
        Soldier = null;
        Troop = null;
    }

    public void StartRepickDelay(float seconds) => _repickDelay = seconds;

    public void Tick(float deltaTime)
    {
        if (deltaTime > 0f) _repickDelay -= deltaTime;
    }
}
