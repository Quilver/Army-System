using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

// Campaign characters, character classes and the migration that converts existing character assets
// (CampaignCharacterMigration). The migration test works on assets in a temporary folder, deleted afterwards.
public sealed class CampaignCharacterTests
{
    const string TempFolder = "Assets/__CampaignCharacterTests";

    readonly List<Object> _created = new();

    [TearDown]
    public void TearDown()
    {
        foreach (var target in _created)
            if (target != null) Object.DestroyImmediate(target);
        _created.Clear();
        if (AssetDatabase.IsValidFolder(TempFolder)) AssetDatabase.DeleteAsset(TempFolder);
    }

    [Test]
    public void Defaults_GrowthIs20_CapIs20_HpCapIs40_OneWeaponSkillCap()
    {
        var growths = new StatGrowths();
        var caps = new StatCaps();
        foreach (CharacterStat stat in System.Enum.GetValues(typeof(CharacterStat)))
        {
            Assert.AreEqual(20, growths.For(stat), stat.ToString());
            if (stat == CharacterStat.WeaponSkill)
            {
                Assert.Throws<System.ArgumentException>(() => caps.For(stat));
                continue;
            }
            int expectedCap = stat == CharacterStat.MaxHealth ? 40 : 20;
            Assert.AreEqual(expectedCap, caps.For(stat), stat.ToString());
            Assert.AreEqual(expectedCap, StatCaps.DefaultFor(stat), stat.ToString());
        }

        Assert.AreEqual(1, caps.WeaponSkills.Count);
        Assert.AreEqual(WeaponClass.Sword, caps.WeaponSkills[0].Class);
        Assert.AreEqual(20, caps.WeaponSkills[0].Cap);
        Assert.AreEqual(20, caps.ForWeapon(WeaponClass.Sword));
        Assert.AreEqual(20, caps.ForWeapon(WeaponClass.Bow), "An unlisted weapon class uses the default cap.");
    }

    [Test]
    public void ClassName_ComesFromClassAsset_OrFallsBackToCharacterName()
    {
        var weapon = Track(MeleeWeaponProfile.CreateRuntimeProfile("Sword", MeleeWeaponClass.Sword, 0, 0,
            null));
        var logger = Track(CharacterClass.CreateRuntimeClass("Logger"));
        var withClass = Track(CharacterProfile.CreateRuntimeProfile("Brom", logger, null, null, 12, 5, 5,
            new CharacterEquipment(weapon, null, null), null));
        var withoutClass = Track(CharacterProfile.CreateRuntimeProfile("Nobody", null, null, null, 12, 5, 5,
            new CharacterEquipment(weapon, null, null), null));

        Assert.AreSame(logger, withClass.Class);
        Assert.AreEqual("Logger", withClass.ClassName);
        Assert.AreEqual("Nobody", withoutClass.ClassName);
    }

    [Test]
    public void Migration_ConvertsCharacterInPlace_KeepingStatsGuidAndTroopReference()
    {
        AssetDatabase.CreateFolder("Assets", "__CampaignCharacterTests");
        string uniqueClass = "Test Class " + System.Guid.NewGuid().ToString("N");

        var weapon = MeleeWeaponProfile.CreateRuntimeProfile("Axe", MeleeWeaponClass.Axe, 4, 2, null);
        AssetDatabase.CreateAsset(weapon, $"{TempFolder}/Axe.asset");
        var stats = new SoldierStats(6, 7, 8, 4, 6, 6, 22, new List<WeaponSkill> { new(WeaponClass.Axe, 5) });
        var character = CharacterProfile.CreateRuntimeProfile("Brom", null, null, null, 12, 5, 3,
            new CharacterEquipment(weapon, null, null), stats);
        string characterPath = $"{TempFolder}/Brom.asset";
        AssetDatabase.CreateAsset(character, characterPath);
        // An old asset: the class is still the legacy string.
        var so = new SerializedObject(character);
        so.FindProperty("_className").stringValue = uniqueClass;
        so.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
        string guid = AssetDatabase.AssetPathToGUID(characterPath);

        Assert.AreEqual(1, CampaignCharacterMigration.MigrateClasses(new[] { TempFolder }, TempFolder));
        Assert.AreEqual(1, CampaignCharacterMigration.ConvertToCampaignCharacters(TempFolder));
        // A second run changes nothing.
        Assert.AreEqual(0, CampaignCharacterMigration.MigrateClasses(new[] { TempFolder }, TempFolder));
        Assert.AreEqual(0, CampaignCharacterMigration.ConvertToCampaignCharacters(TempFolder));

        var campaign = AssetDatabase.LoadAssetAtPath<CampaignCharacter>(characterPath);
        Assert.IsNotNull(campaign, "The character asset should load as a CampaignCharacter.");
        Assert.AreEqual(guid, AssetDatabase.AssetPathToGUID(characterPath));
        Assert.AreEqual("Brom", campaign.CharacterName);
        Assert.AreEqual(12, campaign.Numbers);
        Assert.AreEqual(5, campaign.Morale);
        Assert.AreEqual(3, campaign.Discipline);
        Assert.AreEqual(6, campaign.Speed);
        Assert.AreEqual(7, campaign.Defence);
        Assert.AreEqual(8, campaign.Strength);
        Assert.AreEqual(22, campaign.MaxHealth);
        Assert.AreEqual(5, campaign.SkillFor(WeaponClass.Axe));
        Assert.AreEqual(AssetDatabase.LoadAssetAtPath<MeleeWeaponProfile>($"{TempFolder}/Axe.asset"), campaign.MeleeWeapon);

        Assert.IsNotNull(campaign.Class);
        Assert.AreEqual(uniqueClass, campaign.ClassName);
        Assert.AreEqual(TempFolder, System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(campaign.Class)).Replace('\\', '/'));
        Assert.AreEqual(20, campaign.CapFor(CharacterStat.Strength));
        Assert.AreEqual(40, campaign.CapFor(CharacterStat.MaxHealth));
        Assert.AreEqual(20, campaign.CapFor(CharacterStat.Numbers));
        Assert.AreEqual(20, campaign.CapFor(WeaponClass.Axe));
        foreach (CharacterStat stat in System.Enum.GetValues(typeof(CharacterStat)))
            Assert.AreEqual(StatGrowths.DefaultChance, campaign.GrowthFor(stat), stat.ToString());
    }

    // SU-99: the campaign characters sit in mission subfolders (Campaign/Mission 1, ...). A saved roster names a character
    // by asset name only; it used to be looked up directly in Campaign/, so every saved character was lost on load.
    [Test]
    public void SavedRosterCharacter_IsFoundInAMissionSubfolder()
    {
        CampaignCharacter any = null;
        foreach (var candidate in Resources.LoadAll<CampaignCharacter>(Campaign.RosterCharacter.CharacterFolder))
        {
            string path = AssetDatabase.GetAssetPath(candidate);
            if (!path.Contains(Campaign.RosterCharacter.CharacterFolder + "/" + candidate.name + ".asset")) { any = candidate; break; }
        }
        if (any == null) Assert.Ignore("No campaign character in a subfolder to test with.");

        var saved = JsonUtility.FromJson<Campaign.RosterCharacter>("{\"characterAsset\":\"" + any.name + "\"}");
        Assert.IsNull(saved.character);
        saved.ResolveCharacter();
        Assert.AreSame(any, saved.character, $"{any.name} resolved from {AssetDatabase.GetAssetPath(any)}");
    }

    T Track<T>(T target) where T : Object
    {
        _created.Add(target);
        return target;
    }
}
