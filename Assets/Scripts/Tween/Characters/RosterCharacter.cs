using System.Collections.Generic;
using UnityEngine;

namespace Campaign
{
    // A character in the player's roster, built on the new character system (CampaignCharacter, spec §1.1). Holds the
    // campaign progress the asset does not: level, XP, the stat points gained on level-ups and the gold it costs to
    // field. Pre-battle tent: TentHub.md SU-52. Levels: every XpPerLevel XP; each level-up rolls every stat against its
    // growth chance and adds +1 below the class cap (SU-115).
    [System.Serializable]
    public class RosterCharacter : ISerializationCallbackReceiver
    {
        public const int XpPerLevel = 10;

        // The stats in the order they are rolled and shown on a level-up (the ledger page's order), then weapon skills.
        public static readonly CharacterStat[] GrowthOrder =
        {
            CharacterStat.Numbers, CharacterStat.MaxHealth, CharacterStat.Morale, CharacterStat.Discipline, CharacterStat.Speed,
            CharacterStat.Defence, CharacterStat.Strength, CharacterStat.Support, CharacterStat.Charge, CharacterStat.Aggression,
        };

        // The growth roll: true when a roll succeeds against a chance in %. Tests replace it.
        public static System.Func<int, bool> GrowthRoll = chance => UnityEngine.Random.Range(0, 100) < chance;
        // Resources folder of the campaign characters; saves refer to a character by its asset name in this folder or
        // any folder under it (the characters are grouped by mission: Campaign/Mission 1, Mission 2, ...).
        public const string CharacterFolder = "Troop and Character Profiles/Characters/Campaign";

        public CampaignCharacter character;
        [SerializeField, HideInInspector] string characterAsset;
        [Min(1)] public int level = 1;
        [Range(0, XpPerLevel - 1)] public int xp;
        [Range(5, 50)] public int costToField = 20;
        // Points gained on level-ups, on top of the CampaignCharacter's stats.
        [SerializeField] List<StatGain> statGains = new();
        [SerializeField] List<SkillGain> skillGains = new();

        public event System.Action<int> GainedXp;
        public event System.Action<LevelUpResult> LevelledUp;

        public string Name => character != null ? character.CharacterName : "Unknown";
        public string ClassName => character != null ? character.ClassName : "";
        public Sprite ClassIcon => character != null ? character.ClassIcon : null;
        public Sprite Portrait => character != null ? character.Portrait : null;
        public float XpFraction => Mathf.Clamp01(xp / (float)XpPerLevel);
        // XP earned since level 1.
        public int TotalXp => (level - 1) * XpPerLevel + xp;

        public static float FractionToNextLevel(int totalXp) => (totalXp % XpPerLevel) / (float)XpPerLevel;

        // Adds XP and levels up every XpPerLevel, rolling stat growth each time (SU-115).
        public void AddXp(int amount)
        {
            if (amount <= 0) return;
            GainedXp?.Invoke(amount);
            xp += amount;
            while (xp >= XpPerLevel)
            {
                xp -= XpPerLevel;
                LevelledUp?.Invoke(LevelUp());
            }
        }

        // One level: each stat in GrowthOrder, then each listed weapon skill, rolls its growth chance; a success below the
        // class cap adds +1.
        LevelUpResult LevelUp()
        {
            level++;
            var result = new LevelUpResult(level);
            if (character == null) return result;
            foreach (CharacterStat stat in GrowthOrder)
            {
                int before = Stat(stat);
                bool grew = before < character.CapFor(stat) && GrowthRoll(character.GrowthFor(stat));
                if (grew) AddGain(stat);
                result.Rolls.Add(new StatRoll(stat, default, before, grew));
            }
            foreach (WeaponSkill skill in character.WeaponSkills)
            {
                int before = SkillLevel(skill.Class);
                bool grew = before < character.CapFor(skill.Class) && GrowthRoll(character.GrowthFor(CharacterStat.WeaponSkill));
                if (grew) AddGain(skill.Class);
                result.Rolls.Add(new StatRoll(CharacterStat.WeaponSkill, skill.Class, before, grew));
            }
            return result;
        }

        // --- Stats with level-up gains ---

        // The stat now: the character's value plus the points gained on level-ups. Not for WeaponSkill (use SkillLevel).
        public int Stat(CharacterStat stat) => BaseStat(stat) + GainOf(stat);

        public int SkillLevel(WeaponClass weaponClass) =>
            (character != null ? character.SkillFor(weaponClass) : 0) + GainOf(weaponClass);

        public int GainOf(CharacterStat stat)
        {
            foreach (var gain in statGains) if (gain.stat == stat) return gain.amount;
            return 0;
        }

        public int GainOf(WeaponClass weaponClass)
        {
            foreach (var gain in skillGains) if (gain.weaponClass == weaponClass) return gain.amount;
            return 0;
        }

        int BaseStat(CharacterStat stat)
        {
            var c = character;
            if (c == null) return 0;
            return stat switch
            {
                CharacterStat.Numbers => c.Numbers,
                CharacterStat.MaxHealth => c.MaxHealth,
                CharacterStat.Morale => c.Morale,
                CharacterStat.Discipline => c.Discipline,
                CharacterStat.Speed => c.Speed,
                CharacterStat.Defence => c.Defence,
                CharacterStat.Strength => c.Strength,
                CharacterStat.Support => c.Support,
                CharacterStat.Charge => c.Charge,
                CharacterStat.Aggression => c.Aggression,
                _ => 0,
            };
        }

        void AddGain(CharacterStat stat)
        {
            statGains ??= new List<StatGain>();
            foreach (var gain in statGains)
                if (gain.stat == stat) { gain.amount++; return; }
            statGains.Add(new StatGain { stat = stat, amount = 1 });
        }

        void AddGain(WeaponClass weaponClass)
        {
            skillGains ??= new List<SkillGain>();
            foreach (var gain in skillGains)
                if (gain.weaponClass == weaponClass) { gain.amount++; return; }
            skillGains.Add(new SkillGain { weaponClass = weaponClass, amount = 1 });
        }

        // Level, XP and stats as they are now, for showing a page as it was before rewards were granted.
        public CharacterSnapshot Snapshot()
        {
            var snapshot = new CharacterSnapshot { Level = level, Xp = xp };
            foreach (CharacterStat stat in GrowthOrder) snapshot.Stats[stat] = Stat(stat);
            if (character != null)
                foreach (WeaponSkill skill in character.WeaponSkills) snapshot.Skills[skill.Class] = SkillLevel(skill.Class);
            return snapshot;
        }

        // A saved game stores the asset name only (object references do not survive a JSON save). Called after loading.
        public void ResolveCharacter()
        {
            if (character != null || string.IsNullOrEmpty(characterAsset)) return;
            character = Resources.Load<CampaignCharacter>(CharacterFolder + "/" + characterAsset) ?? FindByName(characterAsset);
            if (character == null) Debug.LogWarning($"Saved roster character '{characterAsset}' was not found in Resources/{CharacterFolder}.");
        }

        // The campaign character with this asset name anywhere under CharacterFolder (Resources.Load needs the full path,
        // so a character in a mission subfolder is not found by name alone; a lost character is dropped from the roster).
        static CampaignCharacter FindByName(string assetName)
        {
            foreach (CampaignCharacter candidate in Resources.LoadAll<CampaignCharacter>(CharacterFolder))
                if (candidate != null && candidate.name == assetName) return candidate;
            return null;
        }

        public void OnBeforeSerialize()
        {
            if (character != null) characterAsset = character.name;
        }

        public void OnAfterDeserialize() { }
    }

    [System.Serializable]
    public class StatGain
    {
        public CharacterStat stat;
        public int amount;
    }

    [System.Serializable]
    public class SkillGain
    {
        public WeaponClass weaponClass;
        public int amount;
    }

    // One stat's roll on a level-up. WeaponClass is set when Stat is WeaponSkill.
    public readonly struct StatRoll
    {
        public readonly CharacterStat Stat;
        public readonly WeaponClass WeaponClass;
        public readonly int Before;
        public readonly bool Grew;

        public StatRoll(CharacterStat stat, WeaponClass weaponClass, int before, bool grew)
        {
            Stat = stat;
            WeaponClass = weaponClass;
            Before = before;
            Grew = grew;
        }

        public bool IsWeaponSkill => Stat == CharacterStat.WeaponSkill;
        public int After => Before + (Grew ? 1 : 0);
    }

    // What one level-up did: the new level and every stat's roll, in GrowthOrder then weapon skills.
    public sealed class LevelUpResult
    {
        public readonly int Level;
        public readonly List<StatRoll> Rolls = new();

        public LevelUpResult(int level) => Level = level;
    }

    // A character's level, XP and stats at one moment (RosterCharacter.Snapshot).
    public sealed class CharacterSnapshot
    {
        public int Level;
        public int Xp;
        public readonly Dictionary<CharacterStat, int> Stats = new();
        public readonly Dictionary<WeaponClass, int> Skills = new();
    }
}
