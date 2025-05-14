using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleReport: MonoBehaviour
{
    static BattleReport instance;
    private void Awake()
    {
        if(instance == null)
            instance = this;
        else 
            Destroy(gameObject);
    }
    [SerializeField]
    float _timeTaken = 0;
    public static float timeTaken
    {
        get { return instance._timeTaken; }
        set { instance._timeTaken = value; }
    }
    [SerializeField]
    int _kills, _deaths = 0; 
    public static int kills
    {
        get { return instance._kills; }
        set { instance._kills = value; }
    }
    public static int deaths
    {
        get { return instance._deaths; }
        set { instance._deaths = value; }
    }
    [SerializeField]
    List<AchievementSystem.Achievments> _achievements;
    public static List<AchievementSystem.Achievments> Achievements
    {
        get
        {
            return instance._achievements;
        }
        set
        {
            instance._achievements = value;
        }
    }
    [SerializeField]
    List<Campaign.PCWrapper> _deployedCharacters;
    public static List<Campaign.PCWrapper> DeployedCharacters
    {
        get { return instance._deployedCharacters; }
        set { instance._deployedCharacters = value; }
    }
    public static UpdateCharacter CharacterUpdate;
    public delegate void UpdateCharacter(Campaign.PCWrapper character);
    public static void Set(List<Campaign.PCWrapper> deployedCharacter, List<AchievementSystem.Achievments> achievements)
    {
        timeTaken = 0;
        kills= 0;
        deaths= 0;
        Achievements = achievements;
        DeployedCharacters = deployedCharacter;
    }
    public static void Reset()
    {
        timeTaken = 0;
        kills = 0;
        deaths = 0;
        Achievements = new();
        DeployedCharacters = new();
    }
}
