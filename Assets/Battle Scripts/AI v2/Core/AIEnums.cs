namespace ArmySystem.AI.V2
{
    public enum TaskStatus
    {
        Inactive,
        Running,
        Succeeded,
        Failed,
        Cancelled
    }

    public enum ArmyPosture
    {
        Defensive,
        Offensive,
        Balanced,
        Withdraw
    }

    public enum InfluenceChannel
    {
        MeleeThreat,
        RangedThreat,
        Observation,
        FriendlySupport,
        MovementControl
    }

    public enum ObjectiveControl
    {
        Neutral,
        Friendly,
        Enemy
    }

    public enum FlankSide
    {
        Left,
        Right
    }
}
