using System.Collections.Generic;
using UnityEngine;

// Results carried from a battle to the campaign screens (Tween system). Lives on the persistent DataManager object.
// Each level scene has its own DataManager too: played on its own, the level uses it (its DeployedTroops are the
// level's default troops); played from the campaign, the persistent one is used and the level's copy destroys itself.
public class BattleReport : MonoBehaviour
{
    static BattleReport _instance;

    // The report in use. Before any report has woken up (a level played on its own, read from another object's Awake
    // such as the deployment zone's) it is the one in the loaded scene.
    static BattleReport instance
    {
        get
        {
            if (_instance == null) _instance = FindAnyObjectByType<BattleReport>();
            return _instance;
        }
    }

    void Awake()
    {
        if (_instance == null)
            _instance = this;
        else if (_instance != this)
            Destroy(gameObject);
    }

    // False when a battle scene has no DataManager and none came from the campaign.
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

    // Characters deployed in the pre-battle tent (SU-52): what they cost, and who earns XP and achievements.
    [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("_deployedRoster")]
    List<Campaign.RosterCharacter> _deployedCharacters = new();
    public static List<Campaign.RosterCharacter> DeployedCharacters
    {
        get { return instance._deployedCharacters; }
        set { instance._deployedCharacters = value; }
    }

    // The troops the player deploys in the battle; the deployment zone spawns them. Each level scene's DataManager
    // lists that level's default troops here, for playing the level on its own. Coming from battle preparation, the
    // persistent DataManager's report is used instead (the level's own copy is destroyed), holding one troop per
    // deployed character (RosterTroops keeps it in step with DeployedCharacters).
    [SerializeField]
    List<TroopProfile> _deployedTroops = new();
    public static List<TroopProfile> DeployedTroops
    {
        get { return instance._deployedTroops; }
        set { instance._deployedTroops = value; }
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
        DeployedCharacters = new();
        DeployedTroops = new();
    }
}
