namespace LeveragedWars.Battle.AI
{
    /// <summary>Which side a troop or objective belongs to, as seen by the AI.</summary>
    public enum AITeam { None, Player, Enemy }

    public static class AITeamExtensions
    {
        public static AITeam Opponent(this AITeam team) =>
            team == AITeam.Player ? AITeam.Enemy :
            team == AITeam.Enemy ? AITeam.Player : AITeam.None;
    }
}
