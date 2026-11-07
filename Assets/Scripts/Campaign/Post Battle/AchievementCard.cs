using UnityEngine;
using UnityEngine.UI;

namespace EndGameUI
{
    // One earned achievement on the post-battle screen. Showing the card grants its rewards (once).
    public class AchievementCard : HoverTip
    {
        [SerializeField]
        Image image;
        AchievementSystem.Achievement achievement;

        public void Init(AchievementSystem.Achievement achievement)
        {
            this.achievement = achievement;
            image.sprite = achievement.icon;
            tip = achievement.HoverText;
            GetReward();
        }

        public void GetReward()
        {
            if (achievement == null || achievement.RewardsGranted) return;
            image.color = new Color(0.8f, 0.8f, 0.8f);
            achievement.GrantRewards();
        }
    }
}
