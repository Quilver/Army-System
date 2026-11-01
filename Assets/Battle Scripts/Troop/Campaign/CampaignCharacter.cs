using System.Collections.Generic;
using UnityEngine;

// A character in the player's roster (campaign only). Adds level-up data to CharacterProfile:
// - Growths: the % chance that each stat goes up on a level-up (StatGrowths, default 20 %).
// - Caps: come from the character's class (CharacterClass.StatCaps). A character without a class uses the StatCaps
//   defaults (20, HP 40). Weapon skills are capped per weapon class.
// - Achievements: set up for the character's troop in each battle it is deployed in (AchievementController).
// The battle system reads only the CharacterProfile part, so a campaign character fights exactly like any other
// character and can sit in any TroopProfile.
[CreateAssetMenu(menuName = "Troops/Campaign Character")]
public sealed class CampaignCharacter : CharacterProfile
{
    [Header("Campaign: growth chances (% per level-up)")]
    [SerializeField] StatGrowths _growths = new();

    public StatGrowths Growths => _growths;

    [Header("Campaign: achievements in every battle this character fights")]
    [SerializeField] List<AchievementSystem.CharacterAchievement> _achievements = new();

    public IReadOnlyList<AchievementSystem.CharacterAchievement> Achievements => _achievements;

    // Growth chance (%) of a stat.
    public int GrowthFor(CharacterStat stat) => _growths.For(stat);

    // The highest value a stat can grow to: the class cap, or the default cap without a class. Weapon skills: use
    // CapFor(WeaponClass).
    public int CapFor(CharacterStat stat) => Class != null ? Class.CapFor(stat) : StatCaps.DefaultFor(stat);

    // The highest level the skill with a weapon class can grow to.
    public int CapFor(WeaponClass weaponClass) => Class != null ? Class.CapFor(weaponClass) : StatCaps.DefaultCap;

    [ContextMenu("Reset Growths To Default")]
    public void ResetGrowthsToDefault() => _growths = new StatGrowths();

    protected override void OnValidate()
    {
        base.OnValidate();
        _growths ??= new StatGrowths();
        _growths.Validate();
        WarnAboveCaps();
    }

    // A stat above its class cap is allowed (the cap only stops growth), but is most likely a data mistake.
    void WarnAboveCaps()
    {
        foreach (var (stat, value) in ScalarStats())
        {
            int cap = CapFor(stat);
            if (value > cap) Debug.LogWarning($"{name}: {stat} {value} is above its class cap {cap}.", this);
        }
        foreach (var skill in WeaponSkills)
        {
            int skillCap = CapFor(skill.Class);
            if (skill.Level > skillCap)
                Debug.LogWarning($"{name}: {skill.Class} skill {skill.Level} is above its class cap {skillCap}.", this);
        }
    }

    IEnumerable<(CharacterStat Stat, int Value)> ScalarStats()
    {
        yield return (CharacterStat.Numbers, Numbers);
        yield return (CharacterStat.Speed, Speed);
        yield return (CharacterStat.Defence, Defence);
        yield return (CharacterStat.Strength, Strength);
        yield return (CharacterStat.Support, Support);
        yield return (CharacterStat.Charge, Charge);
        yield return (CharacterStat.Aggression, Aggression);
        yield return (CharacterStat.MaxHealth, MaxHealth);
        yield return (CharacterStat.Morale, Morale);
        yield return (CharacterStat.Discipline, Discipline);
    }
}
