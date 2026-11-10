using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EndGameUI
{
    // Shows the battle's earned achievements one card at a time. Each card grants its rewards as it appears; leaving the
    // screen early grants the rest (EndGameButton).
    public class AchievementManager : MonoBehaviour
    {
        [SerializeField]
        List<AchievementSystem.Achievement> achievements;
        [SerializeField]
        GameObject AchievementCard;
        [SerializeField, Min(0f)]
        float _secondsBetweenCards = 1f;

        void Start() => StartCoroutine(SlowlyAddAchievements());

        IEnumerator SlowlyAddAchievements()
        {
            achievements = new List<AchievementSystem.Achievement>(BattleReport.Achievements);
            yield return new WaitForSeconds(_secondsBetweenCards);
            foreach (var achievement in achievements)
            {
                if (achievement == null || !achievement.Achieved()) continue;
                var card = Instantiate(AchievementCard, transform);
                card.GetComponent<AchievementCard>().Init(achievement);
                yield return new WaitForSeconds(_secondsBetweenCards);
            }
        }
    }
}
