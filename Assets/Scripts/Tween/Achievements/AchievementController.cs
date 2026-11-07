using System.Collections;
using System.Collections.Generic;
using Campaign;
using UnityEngine;

namespace AchievementSystem
{
    // Sets up and runs the battle's achievements (SU-114, SU-115). One per battle scene.
    // - Global: every battle's (AchievementTuning: Mission Complete), then the assets and inline ones listed here
    //   (victory, side objectives such as capture points).
    // - Character: for each character deployed from the roster, tied to the troop it leads: the every-character ones
    //   (AchievementTuning), its class's mini quests (ClassAchievements), the character's own
    //   (CampaignCharacter.Achievements), and the battle-specific ones listed here.
    // Each is instantiated for this battle, ticked every frame, finished when the battle ends (its result is then fixed)
    // and added to BattleReport.Achievements for the post-battle screen, which grants the rewards.
    public class AchievementController : MonoBehaviour
    {
        [Tooltip("Global achievements of this battle.")]
        public List<GlobalAchievement> _GlobalAchievements = new();
        [Tooltip("Global achievements defined here, for requirements on scene objects (a capture point).")]
        public List<CustomGlobalAchievement> _CustomGlobalAchievements = new();
        [Tooltip("Character achievements every deployed character has in this battle only.")]
        public List<CharacterAchievement> _achievementsPC = new();
        [Tooltip("Character achievements defined here, for every deployed character in this battle only.")]
        public List<CustomCharacterAchievement> _CustomPcAchievements = new();

        readonly List<Achievement> _active = new();
        Battle _battle;
        bool _finished;

        public IReadOnlyList<Achievement> Active => _active;

        void Start() => StartCoroutine(Initialise());

        IEnumerator Initialise()
        {
            // Wait a frame so the deployment zone has spawned the roster's troops (it spawns them in Awake).
            yield return null;

            _battle = Battle.Instance;
            if (_battle == null || !BattleReport.Exists || _battle.IsOver) yield break;
            _battle.BattleOver += OnBattleOver;
            BattleReport.Achievements = new List<Achievement>();
            Deployment.RosterTroops.EnsureDeployedCharacters(_battle.player);
            foreach (var deployed in BattleReport.DeployedCharacters) BattleReport.RecordFor(deployed);

            var battleContext = new AchievementContext(_battle);
            var globals = new HashSet<GlobalAchievement>();
            AchievementTuning tuning = AchievementTuning.Active;
            if (tuning != null)
                foreach (var achievement in tuning.EveryBattle)
                    if (achievement != null && globals.Add(achievement)) Track(achievement, battleContext);
            foreach (var achievement in _GlobalAchievements)
                if (achievement != null && globals.Add(achievement)) Track(achievement, battleContext);
            // The mission sheet's achievements for this battle scene (SU-119).
            var mission = Campaign.Missions.MissionCatalog.ForScene(gameObject.scene.path);
            if (mission != null)
                foreach (var achievement in mission.achievements)
                    if (achievement != null && globals.Add(achievement)) Track(achievement, battleContext);
            foreach (var data in _CustomGlobalAchievements)
            {
                if (data == null) continue;
                GlobalAchievement template = GlobalAchievement.CreateCustom(data);
                Track(template, battleContext);
                Destroy(template);
            }

            // Inline character achievements become temporary templates, removed once every character has its copy.
            var customTemplates = new List<CharacterAchievement>();
            foreach (var data in _CustomPcAchievements)
                if (data != null) customTemplates.Add(CharacterAchievement.CreateCustom(data));
            var battleCharacterAchievements = new List<CharacterAchievement>(_achievementsPC);
            if (mission != null)
                foreach (var achievement in mission.characterAchievements)
                    if (achievement != null && !battleCharacterAchievements.Contains(achievement))
                        battleCharacterAchievements.Add(achievement);
            battleCharacterAchievements.AddRange(customTemplates);

            foreach (RosterCharacter character in BattleReport.DeployedCharacters)
            {
                Troop troop = TroopOf(character);
                if (troop == null) continue;
                var context = new AchievementContext(_battle, character, troop);
                foreach (var achievement in CharacterAchievementsOf(character, battleCharacterAchievements))
                    Track(achievement, context);
            }
            foreach (var template in customTemplates) Destroy(template);
            Debug.Log($"Achievements: tracking {_active.Count} for {BattleReport.DeployedCharacters.Count} deployed character(s).");
        }

        // Every-character ones, the class's, the character's own, then this battle's; each asset once.
        static IEnumerable<CharacterAchievement> CharacterAchievementsOf(RosterCharacter character,
            List<CharacterAchievement> battleAchievements)
        {
            var seen = new HashSet<CharacterAchievement>();
            AchievementTuning tuning = AchievementTuning.Active;
            if (tuning != null)
                foreach (var achievement in tuning.EveryCharacter)
                    if (achievement != null && seen.Add(achievement)) yield return achievement;
            ClassAchievements table = ClassAchievements.Load();
            if (table != null)
                foreach (var achievement in table.For(character.character.Class))
                    if (seen.Add(achievement)) yield return achievement;
            foreach (var achievement in character.character.Achievements)
                if (achievement != null && seen.Add(achievement)) yield return achievement;
            foreach (var achievement in battleAchievements)
                if (achievement != null && seen.Add(achievement)) yield return achievement;
        }

        void Track(Achievement template, AchievementContext context)
        {
            if (template == null) return;
            Achievement achievement = Instantiate(template);
            achievement.name = template.name;
            if (!achievement.Begin(context))
            {
                Destroy(achievement);
                return;
            }
            _active.Add(achievement);
            BattleReport.Achievements.Add(achievement);
        }

        void Update()
        {
            if (_finished) return;
            float deltaTime = Time.deltaTime;
            foreach (var achievement in _active)
                achievement.Tick(deltaTime);
        }

        void OnBattleOver(bool victory)
        {
            if (_finished) return;
            _finished = true;
            int earned = 0;
            foreach (var achievement in _active)
            {
                achievement.Finish(victory);
                if (achievement.Achieved()) earned++;
            }
            Debug.Log($"Achievements: battle over ({(victory ? "won" : "lost")}), {earned} of {_active.Count} earned.");
        }

        void OnDestroy()
        {
            if (_battle != null) _battle.BattleOver -= OnBattleOver;
            foreach (var achievement in _active)
                if (achievement != null) achievement.Abort();
        }

        static Troop TroopOf(RosterCharacter character)
        {
            Army army = Battle.Instance != null ? Battle.Instance.player : null;
            if (army == null || character == null || character.character == null) return null;
            foreach (var troop in army.Troops)
                if (troop != null)
                    foreach (var member in troop.Characters)
                        if (member == character.character) return troop;
            return null;
        }
    }
}
