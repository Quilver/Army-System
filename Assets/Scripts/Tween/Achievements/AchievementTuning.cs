using System.Collections.Generic;
using UnityEngine;

namespace AchievementSystem
{
    // Battle Tuning: the achievements of every battle (SU-115). Asset: Assets/Resources/Battle Tuning/Achievement Tuning.
    // Kept in Tween (it lists achievement assets) though the asset sits with the battle tuning; read through Active.
    // - Every Character: character achievements (mini quests) every deployed character has, whatever its class.
    // - Every Battle: global achievements of every battle, such as Mission Complete.
    // Class mini quests are in Resources/Achievement/Class Achievements; a level adds its own on its AchievementController.
    [CreateAssetMenu(menuName = "Battle Tuning/Achievement Tuning")]
    public sealed class AchievementTuning : ScriptableObject
    {
        public const string AssetName = "Achievement Tuning";
        public const string ResourcePath = "Battle Tuning/" + AssetName;

        [SerializeField, Tooltip("Character achievements every deployed character has in every battle.")]
        List<CharacterAchievement> _everyCharacter = new();
        [SerializeField, Tooltip("Global achievements of every battle.")]
        List<GlobalAchievement> _everyBattle = new();

        public IReadOnlyList<CharacterAchievement> EveryCharacter => _everyCharacter;
        public IReadOnlyList<GlobalAchievement> EveryBattle => _everyBattle;

        static AchievementTuning _active;

        // The asset in Resources, or null when there is none.
        public static AchievementTuning Active
        {
            get
            {
                if (_active == null) _active = Resources.Load<AchievementTuning>(ResourcePath);
                return _active;
            }
        }
    }
}
