using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Creates the Phase 01 sample data from the designer's character sheets (Docs/Battle/Decisions.md, D-01).
// Run from Troops > Phase 01 > Generate Sample Data, or from the command line with -executeMethod.
// - Each sheet becomes one Character asset (Characters/<Name>) and one single-character Troop asset (Characters/Troops/<Name>).
// - Existing assets are updated in place with CopySerialized, so their GUIDs and scene references are kept.
// - Portraits and soldier sprites already assigned to characters are kept.
// - The old Characters/Soldiers folder (from before SoldierProfile was removed) is deleted once it is empty.
// - Troop Tuning is created only when it is missing, so designer edits to it are not overwritten.
// - Each sheet's class name becomes a CharacterClass asset (Classes/<Name>), shared by characters of that class.
// - Campaign characters (CampaignCharacter) found at a sheet's path are left alone.
// - Every sheet value is checked against its range. Out-of-range values are logged as errors and the asset is clamped.
// - Stats not on the sheets are placeholders, and the weapon and armour values are marked Provisional.
// - Melee classes become MeleeWeaponProfile assets and ranged classes RangedWeaponProfile assets (Decisions D-52).
//   Old combined weapon assets are converted first (WeaponProfileMigration). Each sheet must list one melee weapon.
public static class Phase01DataGenerator
{
    const string BaseFolder = "Assets/Resources/Troop and Character Profiles";
    const string CharactersFolder = BaseFolder + "/Characters";
    const string LegacySoldierFolder = CharactersFolder + "/Soldiers";
    const string TroopFolder = CharactersFolder + "/Troops";
    const string WeaponFolder = BaseFolder + "/Equipment/Weapons";
    const string ArmourFolder = BaseFolder + "/Equipment/Armour";

    // Radius for the sample troops. The sheets do not give a radius. 5 matches the existing Swordsmen profile, which has
    // a similar soldier count (Questions.md Q-12, working assumption). Line length is derived from it (L_nat).
    const float SampleTroopRadius = 5f;

    struct WeaponDef
    {
        public string Name;
        public WeaponClass Class;
        public int Damage;
        public int ChargeBonus;
        public float Range;
        public int Accuracy;
        public int FireRate;
        public WeaponSpecialRule Special;
        public bool Provisional;
    }

    struct ArmourDef
    {
        public string Name;
        public int Deflect;
        public int Bruise;
        public int Bleed;
        public int DefenceBonus;
    }

    struct CharacterDef
    {
        public string Name;
        public string ClassName;
        public string Armour;
        public string[] Weapons;
        public (WeaponClass Class, int Level)[] Skills;
        public int Numbers;
        public int Morale;
        public int Discipline;
        public int Speed;
        public int Hp;
        public int Aggression;
        public int Strength;
        public int ChargeBonus;
        public int Support;
    }

    // Melee weapon. Damage is the damage bonus. Zero means the sheet gives no value (provisional placeholder).
    static WeaponDef Melee(string name, WeaponClass weaponClass, int damage = 0, int chargeBonus = 0,
        WeaponSpecialRule special = WeaponSpecialRule.None, bool provisional = true)
    {
        return new WeaponDef
        {
            Name = name, Class = weaponClass, Damage = damage, ChargeBonus = chargeBonus,
            Special = special, Provisional = provisional
        };
    }

    // Ranged weapon with the v0.8 bow baseline (spec §4.5 provisional values). Used where the sheet gives no stats.
    static WeaponDef RangedBaseline(string name, WeaponClass weaponClass)
    {
        return new WeaponDef
        {
            Name = name, Class = weaponClass, Damage = 5, Range = 10f, Accuracy = 4, FireRate = 5, Provisional = true
        };
    }

    static readonly WeaponDef[] Weapons =
    {
        Melee("Gladius", WeaponClass.Sword),
        RangedBaseline("Pilum", WeaponClass.Javelin),
        RangedBaseline("Short Bow", WeaponClass.Bow),
        Melee("Dagger", WeaponClass.Sword),
        Melee("Spiked Club", WeaponClass.Mace),
        Melee("Knife", WeaponClass.Sword),
        RangedBaseline("Crossbow", WeaponClass.Crossbow),
        Melee("Broadsword", WeaponClass.Sword),
        RangedBaseline("Javelin", WeaponClass.Javelin),
        // Spec §1.6 sample. The sheet for Gareth writes "Shortsword", which is the same weapon.
        Melee("Short Sword", WeaponClass.Sword, damage: 5, chargeBonus: 3, provisional: false),
        Melee("Greatsword", WeaponClass.Sword),
        Melee("Felling Axe", WeaponClass.Axe),
        RangedBaseline("Throwing Hatchet", WeaponClass.Javelin),
        // Spec §1.6 Long Bow: damage 10, range 20, accuracy 2, fire rate 10.
        new WeaponDef
        {
            Name = "Longbow", Class = WeaponClass.Bow, Damage = 10, Range = 20f, Accuracy = 2, FireRate = 10,
            Provisional = false
        },
        Melee("Bearded Axe", WeaponClass.Axe),
        RangedBaseline("Throwing Axe", WeaponClass.Javelin),
        Melee("Pike", WeaponClass.Spear),
        Melee("Arming Sword", WeaponClass.Sword),
        // Spec §1.6 Lance: damage 3, charge bonus 8, Couched Lance (double damage while charging).
        Melee("Lance", WeaponClass.Spear, damage: 3, chargeBonus: 8,
            special: WeaponSpecialRule.CouchedLance, provisional: false),
    };

    // Light armour and Heavy armour are the spec §1.6 samples. Every other armour is mapped to the nearest sample and
    // is provisional (Questions.md Q-54). Chainmail + Shield uses the 60/30/10 split from Q-6.
    static readonly ArmourDef[] Armours =
    {
        new ArmourDef { Name = "Light Armour", Deflect = 3, Bruise = 5, Bleed = 2, DefenceBonus = 4 },
        new ArmourDef { Name = "Light Armour + Buckler", Deflect = 3, Bruise = 5, Bleed = 2, DefenceBonus = 4 },
        new ArmourDef { Name = "Light Armour + Round Shield", Deflect = 3, Bruise = 5, Bleed = 2, DefenceBonus = 4 },
        new ArmourDef { Name = "Thick Furs", Deflect = 3, Bruise = 5, Bleed = 2, DefenceBonus = 4 },
        new ArmourDef { Name = "Padded Vest", Deflect = 3, Bruise = 5, Bleed = 2, DefenceBonus = 4 },
        new ArmourDef { Name = "Chainmail + Shield", Deflect = 6, Bruise = 3, Bleed = 1, DefenceBonus = 7 },
        new ArmourDef { Name = "Heavy Mail + Shield", Deflect = 6, Bruise = 3, Bleed = 1, DefenceBonus = 7 },
    };

    // Level 1 characters from the designer's sheets (Decisions D-01). Level is not stored: all sheets are Level 1.
    static readonly CharacterDef[] Characters =
    {
        new CharacterDef
        {
            Name = "Gaius", ClassName = "Legionnaire", Armour = "Chainmail + Shield",
            Weapons = new[] { "Gladius", "Pilum" },
            Skills = new[] { (WeaponClass.Sword, 6), (WeaponClass.Javelin, 4) },
            Numbers = 12, Morale = 6, Discipline = 8, Speed = 5, Hp = 20, Aggression = 4, Strength = 5,
            ChargeBonus = 4, Support = 7
        },
        new CharacterDef
        {
            Name = "Vance", ClassName = "Cavalier", Armour = "Heavy Mail + Shield",
            Weapons = new[] { "Lance", "Arming Sword" },
            Skills = new[] { (WeaponClass.Spear, 6), (WeaponClass.Sword, 5) },
            Numbers = 6, Morale = 6, Discipline = 6, Speed = 9, Hp = 24, Aggression = 7, Strength = 6,
            ChargeBonus = 9, Support = 4
        },
        new CharacterDef
        {
            Name = "Franz", ClassName = "Pike", Armour = "Light Armour",
            Weapons = new[] { "Pike", "Dagger" },
            Skills = new[] { (WeaponClass.Spear, 5), (WeaponClass.Sword, 3) },
            Numbers = 12, Morale = 5, Discipline = 8, Speed = 6, Hp = 18, Aggression = 3, Strength = 5,
            ChargeBonus = 3, Support = 9
        },
        new CharacterDef
        {
            Name = "Robin", ClassName = "Archer", Armour = "Light Armour",
            Weapons = new[] { "Short Bow", "Dagger" },
            Skills = new[] { (WeaponClass.Bow, 6), (WeaponClass.Sword, 4) },
            Numbers = 8, Morale = 4, Discipline = 4, Speed = 6, Hp = 14, Aggression = 5, Strength = 6,
            ChargeBonus = 4, Support = 4
        },
        new CharacterDef
        {
            Name = "Borgo", ClassName = "Thug", Armour = "Padded Vest",
            Weapons = new[] { "Spiked Club", "Knife" },
            Skills = new[] { (WeaponClass.Mace, 5), (WeaponClass.Sword, 3) },
            Numbers = 14, Morale = 4, Discipline = 2, Speed = 5, Hp = 19, Aggression = 7, Strength = 7,
            ChargeBonus = 6, Support = 3
        },
        new CharacterDef
        {
            Name = "Richter", ClassName = "Mercenary", Armour = "Chainmail + Shield",
            Weapons = new[] { "Broadsword", "Crossbow" },
            Skills = new[] { (WeaponClass.Sword, 6), (WeaponClass.Crossbow, 4) },
            Numbers = 10, Morale = 5, Discipline = 6, Speed = 5, Hp = 18, Aggression = 5, Strength = 6,
            ChargeBonus = 5, Support = 6
        },
        new CharacterDef
        {
            Name = "Lyra", ClassName = "Skirmisher", Armour = "Light Armour + Buckler",
            Weapons = new[] { "Javelin", "Short Sword" },
            Skills = new[] { (WeaponClass.Javelin, 6), (WeaponClass.Sword, 5) },
            Numbers = 10, Morale = 4, Discipline = 4, Speed = 7, Hp = 15, Aggression = 6, Strength = 4,
            ChargeBonus = 5, Support = 3
        },
        new CharacterDef
        {
            Name = "Kaelen", ClassName = "Sword Master", Armour = "Light Armour",
            Weapons = new[] { "Greatsword" },
            Skills = new[] { (WeaponClass.Sword, 8) },
            Numbers = 8, Morale = 6, Discipline = 7, Speed = 7, Hp = 18, Aggression = 8, Strength = 6,
            ChargeBonus = 6, Support = 4
        },
        new CharacterDef
        {
            Name = "Brom", ClassName = "Logger", Armour = "Thick Furs",
            Weapons = new[] { "Felling Axe", "Throwing Hatchet" },
            Skills = new[] { (WeaponClass.Axe, 5), (WeaponClass.Javelin, 3) },
            Numbers = 12, Morale = 5, Discipline = 3, Speed = 5, Hp = 22, Aggression = 6, Strength = 8,
            ChargeBonus = 6, Support = 4
        },
        new CharacterDef
        {
            Name = "Gareth", ClassName = "Longbowmen", Armour = "Light Armour",
            Weapons = new[] { "Longbow", "Short Sword" },
            Skills = new[] { (WeaponClass.Bow, 7), (WeaponClass.Sword, 4) },
            Numbers = 8, Morale = 5, Discipline = 6, Speed = 5, Hp = 15, Aggression = 4, Strength = 7,
            ChargeBonus = 3, Support = 5
        },
        new CharacterDef
        {
            Name = "Skarde", ClassName = "Raider", Armour = "Light Armour + Round Shield",
            Weapons = new[] { "Bearded Axe", "Throwing Axe" },
            Skills = new[] { (WeaponClass.Axe, 6), (WeaponClass.Javelin, 4) },
            Numbers = 12, Morale = 4, Discipline = 3, Speed = 7, Hp = 17, Aggression = 8, Strength = 6,
            ChargeBonus = 7, Support = 3
        },
    };

    [MenuItem("Troops/Phase 01/Generate Sample Data")]
    public static void GenerateSampleData()
    {
        EnsureFolder("Assets/Resources");
        EnsureFolder(BaseFolder);
        EnsureFolder(BaseFolder + "/Equipment");
        EnsureFolder(WeaponFolder);
        EnsureFolder(ArmourFolder);
        EnsureFolder(CharactersFolder);
        EnsureFolder(TroopFolder);

        int rangeErrors = 0;
        foreach (CharacterDef character in Characters)
            rangeErrors += ValidateCharacter(character);

        // Old combined weapon assets must be melee or ranged assets before they can be updated in place.
        WeaponProfileMigration.Run();

        var weaponAssets = new Dictionary<string, WeaponProfile>();
        foreach (WeaponDef def in Weapons)
        {
            string weaponPath = $"{WeaponFolder}/{def.Name}.asset";
            if (def.Class.IsRanged())
            {
                RangedWeaponProfile fresh = RangedWeaponProfile.CreateRuntimeProfile(
                    def.Name, def.Class.ToRangedClass(), def.Damage, def.Range, def.Accuracy, def.FireRate, def.Provisional);
                weaponAssets[def.Name] = CreateOrUpdate(weaponPath, fresh);
            }
            else
            {
                MeleeWeaponProfile fresh = MeleeWeaponProfile.CreateRuntimeProfile(
                    def.Name, def.Class.ToMeleeClass(), def.Damage, def.ChargeBonus,
                    def.Special == WeaponSpecialRule.None ? null : new[] { new WeaponSpecialRuleEntry(def.Special) },
                    def.Provisional);
                weaponAssets[def.Name] = CreateOrUpdate(weaponPath, fresh);
            }
        }

        var armourAssets = new Dictionary<string, ArmourProfile>();
        foreach (ArmourDef def in Armours)
        {
            ArmourProfile fresh = ArmourProfile.CreateRuntimeProfile(
                def.Name, def.Deflect, def.Bruise, def.Bleed, def.DefenceBonus, provisional: true);
            armourAssets[def.Name] = CreateOrUpdate($"{ArmourFolder}/{def.Name}.asset", fresh);
        }

        // Created once and never overwritten, so designer edits to the tuning values survive re-running the generator.
        BattleTuningAssets.EnsureAssets();

        int defence = CombatStatLimits.Default;

        foreach (CharacterDef character in Characters)
        {
            string characterPath = $"{CharactersFolder}/{character.Name}.asset";
            CharacterProfile existing = AssetDatabase.LoadAssetAtPath<CharacterProfile>(characterPath);
            // CopySerialized would turn a campaign character back into a plain one and drop its growths.
            if (existing is CampaignCharacter)
            {
                Debug.LogWarning($"{characterPath} is a campaign character; not overwritten. Edit it in the Inspector.");
                continue;
            }
            Sprite portrait = existing != null ? existing.Portrait : null;
            Sprite soldierSprite = existing != null ? existing.SoldierSprite : null;

            MeleeWeaponProfile melee = null;
            RangedWeaponProfile ranged = null;
            foreach (string weaponName in character.Weapons)
            {
                WeaponProfile weapon = weaponAssets[weaponName];
                if (weapon is MeleeWeaponProfile meleeWeapon && melee == null) melee = meleeWeapon;
                else if (weapon is RangedWeaponProfile rangedWeapon && ranged == null) ranged = rangedWeapon;
                else Debug.LogWarning($"{character.Name}: {weaponName} ignored. A character carries one melee weapon and at most one ranged weapon (D-52).");
            }
            if (melee == null)
            {
                Debug.LogError($"{character.Name}: the sheet lists no melee weapon. Every character must carry one; skipped.");
                continue;
            }

            var skills = new List<WeaponSkill>();
            foreach (var skill in character.Skills)
                skills.Add(new WeaponSkill(skill.Class, skill.Level));

            ArmourProfile armour = armourAssets[character.Armour];

            var stats = new SoldierStats(character.Speed, defence, character.Strength, character.Support,
                character.ChargeBonus, character.Aggression, character.Hp, skills);
            CharacterProfile characterFresh = CharacterProfile.CreateRuntimeProfile(
                character.Name, CampaignCharacterMigration.FindOrCreateClass(character.ClassName), portrait, soldierSprite, character.Numbers, character.Morale,
                character.Discipline, new CharacterEquipment(melee, ranged, armour), stats);
            CharacterProfile characterAsset = CreateOrUpdate(characterPath, characterFresh);

            TroopProfile troopFresh = TroopProfile.CreateRuntimeProfile(
                new List<CharacterProfile> { characterAsset }, SampleTroopRadius);
            CreateOrUpdate($"{TroopFolder}/{character.Name}.asset", troopFresh);
        }

        DeleteLegacySoldierFolderIfEmpty();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Phase 01 sample data: {Characters.Length} characters, {Weapons.Length} weapons, {Armours.Length} armour. " +
                  $"Sheet values out of range: {rangeErrors}.");
    }

    // Checks each sheet value against its range (integer 1-20, HP 10-40). Returns the number of out-of-range values.
    static int ValidateCharacter(CharacterDef character)
    {
        int errors = 0;
        void Check(int value, int min, int max, string label)
        {
            if (value >= min && value <= max) return;
            errors++;
            Debug.LogError($"{character.Name}: {label} = {value} is outside {min}-{max}. It will be clamped.");
        }

        Check(character.Numbers, 0, int.MaxValue, "Numbers");
        Check(character.Morale, CombatStatLimits.Min, CombatStatLimits.Max, "Morale");
        Check(character.Discipline, CombatStatLimits.Min, CombatStatLimits.Max, "Discipline");
        Check(character.Speed, CombatStatLimits.Min, CombatStatLimits.Max, "Speed");
        Check(character.Hp, CombatStatLimits.HpMin, CombatStatLimits.HpMax, "HP");
        Check(character.Aggression, CombatStatLimits.Min, CombatStatLimits.Max, "Aggression");
        Check(character.Strength, CombatStatLimits.Min, CombatStatLimits.Max, "Strength");
        Check(character.ChargeBonus, CombatStatLimits.Min, CombatStatLimits.Max, "Charge");
        Check(character.Support, CombatStatLimits.Min, CombatStatLimits.Max, "Support");
        foreach (var skill in character.Skills)
            Check(skill.Level, CombatStatLimits.Min, CombatStatLimits.Max, $"{skill.Class} skill");
        return errors;
    }

    // Updates an existing asset in place (keeping its GUID) or creates it at the path.
    static T CreateOrUpdate<T>(string path, T fresh) where T : ScriptableObject
    {
        T target = AssetDatabase.LoadAssetAtPath<T>(path);
        if (target == null)
        {
            AssetDatabase.CreateAsset(fresh, path);
            target = fresh;
        }
        else
        {
            EditorUtility.CopySerialized(fresh, target);
            Object.DestroyImmediate(fresh);
        }

        // Neither CreateAsset nor CopySerialized sets the object name from the file name. Without this, every generated
        // asset has an empty name in the Project window, and WeaponProfile.DisplayName falls back to the empty name.
        target.name = Path.GetFileNameWithoutExtension(path);
        EditorUtility.SetDirty(target);
        return target;
    }

    // Old SoldierProfile assets were moved up to Characters/ (they keep their GUIDs and load as characters).
    static void DeleteLegacySoldierFolderIfEmpty()
    {
        if (!AssetDatabase.IsValidFolder(LegacySoldierFolder)) return;
        if (AssetDatabase.FindAssets("t:Object", new[] { LegacySoldierFolder }).Length > 0)
        {
            Debug.LogWarning($"{LegacySoldierFolder} still holds assets. Move or delete them; SoldierProfile no longer exists.");
            return;
        }
        AssetDatabase.DeleteAsset(LegacySoldierFolder);
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        string name = Path.GetFileName(path);
        AssetDatabase.CreateFolder(parent, name);
    }
}
