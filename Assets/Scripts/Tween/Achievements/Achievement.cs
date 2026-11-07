using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AchievementSystem
{
    // An achievement: a requirement measured during a battle and the rewards it gives (SU-114). Two kinds share this base:
    // - CharacterAchievement: tracked on the troop a deployed character leads; its XP goes to that character. These are
    //   the characters' "mini quests" and replace XP for kills.
    // - GlobalAchievement: tracked for the whole battle (victory, side objectives); gold, prestige, campaign rewards.
    // The asset is a template. Each battle the AchievementController instantiates it, so the copy (and its requirement)
    // holds that battle's state and the asset is never changed. The post-battle screen grants the rewards.
    public abstract class Achievement : ScriptableObject
    {
        public Sprite icon;
        [SerializeField, Tooltip("Short name for the post-battle card. Falls back to the asset name.")]
        string _title;
        [SerializeField, TextArea, Tooltip("Character achievements: {name} is replaced with the character's name; " +
            "without it the name is put in front.")]
        string _description;
        [SerializeReference, SubclassSelector]
        List<Reward> rewards = new();

        [NonSerialized] AchievementContext _context;
        [NonSerialized] bool _rewardsGranted;
        [NonSerialized] int _forcedLevel = -1;

        public abstract Requirement Requirement { get; }

        public string Title => string.IsNullOrEmpty(_title) ? name.Replace("(Clone)", "").Trim() : _title;
        public virtual string Description => _description;
        public IReadOnlyList<Reward> Rewards => rewards;

        // The battle this copy is tracking, and the character for a character achievement (null for a global one).
        public AchievementContext Context => _context;
        public Campaign.RosterCharacter Character => _context?.Character;

        // 0: not earned. 1 or more: earned; rewards are multiplied by the level (repeatable achievements).
        public int Level => _forcedLevel >= 0 ? _forcedLevel
            : Requirement != null ? Mathf.Max(0, Requirement.Level) : 0;
        public bool Achieved() => Level > 0;
        public bool RewardsGranted => _rewardsGranted;

        // --- Battle (AchievementController) ---

        // Starts tracking. False when the requirement does not apply here (e.g. a character requirement without a troop).
        public bool Begin(AchievementContext context)
        {
            _context = context;
            return Requirement != null && Requirement.Begin(context);
        }

        public void Tick(float deltaTime) => Requirement?.Tick(deltaTime);

        // Debug and tests: this copy counts as earned at level, for context, without tracking a battle (SU-116).
        public void ForceEarned(AchievementContext context, int level = 1)
        {
            _context = context;
            _forcedLevel = Mathf.Max(0, level);
        }
        public void Finish(bool victory) => Requirement?.Finish(victory);
        // Stops listening without finishing (the battle scene closed before the battle ended).
        public void Abort() => Requirement?.Abort();

        // --- Rewards (post-battle) ---

        // Gives the rewards once, at the achievement's level. Does nothing when it was not earned.
        public void GrantRewards()
        {
            if (_rewardsGranted || !Achieved()) return;
            _rewardsGranted = true;
            var context = new RewardContext(this, Character, Level);
            foreach (var reward in rewards)
                reward?.Grant(context);
        }

        // Grants every earned achievement that has not been granted yet (leaving the post-battle screen early).
        public static void GrantAll(IEnumerable<Achievement> achievements)
        {
            if (achievements == null) return;
            foreach (var achievement in achievements)
                if (achievement != null) achievement.GrantRewards();
        }

        // Gold and XP of the rewards at the current level.
        public int GoldReward
        {
            get
            {
                int total = 0;
                foreach (var reward in rewards) if (reward != null) total += reward.GoldFor(Mathf.Max(1, Level));
                return total;
            }
        }

        public int XpReward
        {
            get
            {
                int total = 0;
                foreach (var reward in rewards) if (reward != null) total += reward.XpFor(Mathf.Max(1, Level));
                return total;
            }
        }

        // "+2 XP, +10 Gold" at the current level.
        public string RewardSummary
        {
            get
            {
                var text = new StringBuilder();
                int level = Mathf.Max(1, Level);
                foreach (var reward in rewards)
                {
                    string line = reward?.Describe(level);
                    if (string.IsNullOrEmpty(line)) continue;
                    if (text.Length > 0) text.Append(", ");
                    text.Append(line);
                }
                return text.ToString();
            }
        }

        // Hover text for the post-battle card.
        public string HoverText
        {
            get
            {
                var text = new StringBuilder(Title);
                if (Level > 1) text.Append(" x").Append(Level);
                if (!string.IsNullOrEmpty(Description)) text.Append('\n').Append(Description);
                string summary = RewardSummary;
                if (!string.IsNullOrEmpty(summary)) text.Append('\n').Append(summary);
                return text.ToString();
            }
        }

        // For the CreateCustom helpers (achievements defined inline on the AchievementController).
        protected void SetData(Sprite iconSprite, string title, string description, List<Reward> rewardList)
        {
            icon = iconSprite;
            _title = title;
            _description = description;
            rewards = rewardList ?? new List<Reward>();
        }
    }

    // Data of an achievement defined inline on the AchievementController instead of as an asset.
    [Serializable]
    public abstract class CustomAchievementData
    {
        [SerializeField] public Sprite _icon;
        [SerializeField] public string _title;
        [SerializeReference, SubclassSelector] public List<Reward> rewards;
        [SerializeField, TextArea] public string _description;
    }
}
