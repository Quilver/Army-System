using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace EndGameUI
{
    public class AchievementManager : MonoBehaviour
    {
        [SerializeField]
        List<AchievementSystem.Achievement> achievements;
        [SerializeField]
        GameObject AchievementCard;
        // Start is called before the first frame update
        void Start()
        {
            
            StartCoroutine(SlowlyAddAchievements());
        }
        private void UpdateInstant()
        {
            achievements = BattleReport.Achievements;
            foreach (var achievement in achievements)
            {
                if(!achievement.Achieved())continue;
                var card = Instantiate(AchievementCard, transform);
                card.GetComponent<AchievementCard>().Init(achievement);
            }
        }
        IEnumerator SlowlyAddAchievements()
        {
            achievements = BattleReport.Achievements;
            yield return new WaitForSeconds(1);
            foreach (var achievement in achievements)
            {
                if (!achievement.Achieved()) continue;
                var card = Instantiate(AchievementCard, transform);
                card.GetComponent<AchievementCard>().Init(achievement);
                yield return new WaitForSeconds(1);
            }
        }

        
    }
}