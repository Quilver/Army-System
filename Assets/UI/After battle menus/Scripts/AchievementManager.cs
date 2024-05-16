using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace EndGameUI
{
    public class AchievementManager : MonoBehaviour
    {
        [SerializeField]
        List<AchievementSystem.Achievments> achievments;
        [SerializeField]
        GameObject AchievementCard;
        // Start is called before the first frame update
        void Start()
        {
            achievments = BattleReport.Achievements;
            foreach (var achievement in achievments)
            {
                var card = Instantiate(AchievementCard, transform);
                card.GetComponent<AchievementCard>().Init(achievement);
            }

        }

        
    }
}