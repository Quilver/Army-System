using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EndGameUI
{
    public class AchievementCard : HoverTip
    {
        [SerializeField]
        Image image;
        AchievementSystem.Achievments achievement;
        public void Init(AchievementSystem.Achievments achievement)
        {
            this.achievement = achievement;
            image.sprite = achievement.icon;
            tip = achievement.Description;
        }
    }
}