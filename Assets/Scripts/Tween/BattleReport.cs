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

    // The troops the player deploys in the battle, each as the list of characters that make it up (leader first). The
    // deployment zone passes each list to a troop, which sets its frontage from it when the battle starts (SU-117).
    // Each level scene's DataManager lists that level's default troops here, for playing the level on its own. Coming
    // from battle preparation, the persistent DataManager's report is used instead (the level's own copy is destroyed),
    // holding one troop per deployed character or pairing (RosterTroops keeps it in step with DeployedCharacters).
    [SerializeField]
    List<DeployedTroop> _deployedTroops = new();
    public static List<DeployedTroop> DeployedTroops
    {
        get { return instance._deployedTroops ??= new List<DeployedTroop>(); }
        set { instance._deployedTroops = value; }
    }

    // The deployed troops as the character lists the battle's deployment zone hands to its troops.
    public static List<IReadOnlyList<CharacterProfile>> DeployedTroopCharacters()
    {
        var result = new List<IReadOnlyList<CharacterProfile>>();
        foreach (DeployedTroop troop in DeployedTroops)
            if (troop != null && troop.FirstCharacter != null) result.Add(troop.Characters);
        return result;
    }

    // Composite troops formed in the tent (SU-117): each deployed character that was dragged onto another one, mapped to
    // the character it serves under. The two fight as one troop led by the leader (RosterTroops.Sync). In memory only:
    // the keys and values are the roster's own RosterCharacter objects.
    [System.NonSerialized]
    Dictionary<Campaign.RosterCharacter, Campaign.RosterCharacter> _deployedLeaders = new();
    public static Dictionary<Campaign.RosterCharacter, Campaign.RosterCharacter> DeployedLeaders =>
        instance._deployedLeaders ??= new Dictionary<Campaign.RosterCharacter, Campaign.RosterCharacter>();

    // Morale breaks: troops of each side that routed (SU-115).
    [SerializeField]
    int _enemyTroopsRouted, _playerTroopsRouted;
    public static int EnemyTroopsRouted
    {
        get { return instance._enemyTroopsRouted; }
        set { instance._enemyTroopsRouted = value; }
    }
    public static int PlayerTroopsRouted
    {
        get { return instance._playerTroopsRouted; }
        set { instance._playerTroopsRouted = value; }
    }

    // Kills and casualties of each deployed character's troop (SU-115). Kept in memory only: the records refer to the
    // roster's own RosterCharacter objects.
    [System.NonSerialized]
    List<CharacterBattleRecord> _characterRecords = new();
    public static IReadOnlyList<CharacterBattleRecord> CharacterRecords => instance._characterRecords;

    // The record of a deployed character, created on first use. Null for a null character.
    public static CharacterBattleRecord RecordFor(Campaign.RosterCharacter character)
    {
        if (character == null) return null;
        foreach (var record in instance._characterRecords)
            if (record.Character == character) return record;
        var created = new CharacterBattleRecord(character);
        instance._characterRecords.Add(created);
        return created;
    }

    public static void ResetBattleStats()
    {
        timeTaken = 0;
        kills = 0;
        deaths = 0;
        EnemyTroopsRouted = 0;
        PlayerTroopsRouted = 0;
        instance._characterRecords = new List<CharacterBattleRecord>();
    }

    // Raised when a character's campaign data changes after the battle (XP from an achievement).
    public static UpdateCharacter CharacterUpdate;
    public delegate void UpdateCharacter(Campaign.RosterCharacter character);

    public static void Reset()
    {
        ResetBattleStats();
        Achievements = new();
        DeployedCharacters = new();
        DeployedLeaders.Clear();
        DeployedTroops = new();
    }
}

// One troop deployed in the battle: the characters that make it up, leader first. One character is an ordinary troop,
// two or more a composite troop (spec §1.2).
[System.Serializable]
public sealed class DeployedTroop
{
    [SerializeField, Tooltip("The characters in this troop. The first one names the troop and gives its portrait.")]
    List<CharacterProfile> _characters = new();

    public DeployedTroop() { }
    public DeployedTroop(IEnumerable<CharacterProfile> characters) =>
        _characters = characters != null ? new List<CharacterProfile>(characters) : new List<CharacterProfile>();

    public List<CharacterProfile> Characters => _characters ??= new List<CharacterProfile>();

    // The first character, or null when the troop has none.
    public CharacterProfile FirstCharacter
    {
        get
        {
            foreach (var character in Characters)
                if (character != null) return character;
            return null;
        }
    }
}

// One deployed character's battle (SU-115): enemy soldiers its troop killed, its own soldiers lost, whether it routed.
public sealed class CharacterBattleRecord
{
    public readonly Campaign.RosterCharacter Character;
    public int Kills;
    public int Casualties;
    public bool Routed;

    public CharacterBattleRecord(Campaign.RosterCharacter character) => Character = character;
}
