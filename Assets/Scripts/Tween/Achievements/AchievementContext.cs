using Campaign;

namespace AchievementSystem
{
    // What an achievement is tracked for in one battle. A character achievement has the deployed character and the
    // troop it leads; a global achievement has neither.
    public sealed class AchievementContext
    {
        public readonly Battle Battle;
        public readonly RosterCharacter Character;
        public readonly Troop Troop;

        public AchievementContext(Battle battle, RosterCharacter character = null, Troop troop = null)
        {
            Battle = battle;
            Character = character;
            Troop = troop;
        }

        public Army PlayerArmy => Battle != null ? Battle.player : null;
        public Army EnemyArmy => Battle != null ? Battle.enemy : null;
    }
}
