using System.Collections.Generic;
using UnityEngine;

// Results carried from a battle to the campaign screens (Tween system). Lives on the persistent DataManager object.
public class BattleReport : MonoBehaviour
{
    static BattleReport instance;

    void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    // False when a battle scene is played on its own, without the campaign's DataManager.
    public static bool Exists => instance != null;

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
    List<AchievementSystem.Achievement> _achievements;
    public static List<AchievementSystem.Achievement> Achievements
    {
        get
        {
            if (instance._achievements == null) instance._achievements = new();
            return instance._achievements;
        }
        set
        {
            instance._achievements = value;
        }
    }

    // Characters deployed in the pre-battle tent (SU-52). Deployment spawns one troop per character.
    [SerializeField]
    List<Campaign.RosterCharacter> _deployedRoster = new();
    public static List<Campaign.RosterCharacter> DeployedRoster
    {
        get { return instance._deployedRoster; }
        set { instance._deployedRoster = value; }
    }

    // Raised when a character's campaign data changes after the battle (XP from an achievement).
    public static UpdateCharacter CharacterUpdate;
    public delegate void UpdateCharacter(Campaign.RosterCharacter character);

    public static void Reset()
    {
        timeTaken = 0;
        kills = 0;
        deaths = 0;
        Achievements = new();
        DeployedRoster = new();
    }
}
