using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AchievementSystem
{
    public class AchievementController : MonoBehaviour
    {
        public List<CharacterAchievement> _achievementsPC;
        public List<GlobalAchievement> _GlobalAchievements;
        public List<CustomGlobalAchievement> _CustomGlobalAchievements;
        public List<CustomCharacterAchievement> _CustomPcAchievements;
        void Start() {
            StartCoroutine(InitAchievements());
        }
        IEnumerator InitAchievements()
        {
            //Add custom achievements to to Achievements
            foreach (CustomCharacterAchievement achievement in _CustomPcAchievements)
                _achievementsPC.Add(CharacterAchievement.CreateCustom(achievement));
            foreach (CustomGlobalAchievement achievement in _CustomGlobalAchievements)
                _GlobalAchievements.Add(GlobalAchievement.CreateCustom(achievement));

            yield return null;
            //Setup Achievements
            foreach (var unit in Battle.Instance.player.Units)
                foreach (var pc in BattleReport.DeployedCharacters)
                    if (pc.statBase.UnitName == unit.Stats.UnitName)
                        foreach (var achievement in _achievementsPC)
                        {
                            CharacterAchievement pcAchievement = Instantiate(achievement);
                            pcAchievement.Initialise(unit, pc);
                        }


            foreach (var achievement in _GlobalAchievements)
                achievement.Initialise();
            
        }
    }
}