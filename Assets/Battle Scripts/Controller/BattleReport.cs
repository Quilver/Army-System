using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BattleReport
{
    public static float timeTaken=0;
    public static int kills, deaths;
    public static List<AchievementSystem.Achievments> Achievements;
    public static List<Campaign.StatWrapper> statWrappers;
    public static void Reset()
    {
        timeTaken = 0;
        kills= 0;
        deaths= 0;
        Achievements = new();
    }
}
