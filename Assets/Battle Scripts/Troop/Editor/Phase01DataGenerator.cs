using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Creates the Phase 01 sample data from the designer's character sheets (Docs/Battle/Decisions.md, D-01).
// Run from Troops > Phase 01 > Generate Sample Data, or from the command line with -executeMethod.
// - Existing assets are updated in place with CopySerialized, so their GUIDs and scene references are kept.
// - Sprites already assigned to soldier profiles are kept.
// - Troop Tuning is created only when it is missing, so designer edits to it are not overwritten.
// - Every sheet value is checked against its range. Out-of-range values are logged as errors and the asset is clamped.
// - Stats not on the sheets are placeholders, and the weapon and armour values are marked Provisional.
public static class Phase01DataGenerator
{
    const string BaseFolder = "Assets/Resources/Boid Profiles";
    const string CharactersFolder = BaseFolder + "/Characters";
    const string SoldierFolder = CharactersFolder + "/Soldiers";
    const string TroopFolder = CharactersFolder + "/Troops";
    const string WeaponFolder = BaseFolder + "/Equipment/Weapons";
    const string ArmourFolder = BaseFolder + "/Equipment/Armour";
    const string TuningPath = BaseFolder + "/Troop Tuning.asset";

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
        // Spec §1.6 Lance: damage 3, charge bonus 8, double damage while charging.
        Melee("Lance", WeaponClass.Spear, damage: 3, chargeBonus: 8,
            special: WeaponSpecialRule.LanceChargeDouble, provisional: false),
    };

    // Light armour and Heavy armour are the spec §1.6 samples. Every other armour is mapped to the nearest sample and
    // is provisional (Questions.md Q-54). Chainmail + Shield uses the 60/30/10 split from Q-6.
    static readonly ArmourDef[] Armours =
    {
        new ArmourDef { Name = "Light Armour", Deflect = 30, Bruise = 50, Bleed = 20, DefenceBonus = 4 },
        new ArmourDef { Name = "Light Armour + Buckler", Deflect = 30, Bruise = 50, Bleed = 20, DefenceBonus = 4 },
        new ArmourDef { Name = "Light Armour + Round Shield", Deflect = 30, Bruise = 50, Bleed = 20, DefenceBonus = 4 },
        new ArmourDef { Name = "Thick Furs", Deflect = 30, Bruise = 50, Bleed = 20, DefenceBonus = 4 },
        new ArmourDef { Name = "Padded Vest", Deflect = 30, Bruise = 50, Bleed = 20, DefenceBonus = 4 },
        new ArmourDef { Name = "Chainmail + Shield", Deflect = 60, Bruise = 30, Bleed = 10, DefenceBonus = 7 },
        new ArmourDef { Name = "Heavy Mail + Shield", Deflect = 60, Bruise = 30, Bleed = 10, DefenceBonus = 7 },
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
        EnsureFolder(SoldierFolder);
        EnsureFolder(TroopFolder);

        int rangeErrors = 0;
        foreach (CharacterDef character in Characters)
            rangeErrors += ValidateCharacter(character);

        var weaponAssets = new Dictionary<string, WeaponProfile>();
        foreach (WeaponDef def in Weapons)
        {
            WeaponProfile fresh = WeaponProfile.CreateRuntimeProfile(
                def.Name, def.Class, def.Damage, def.ChargeBonus, def.Range, def.Accuracy, def.FireRate,
                def.Special, def.Provisional);
            weaponAssets[def.Name] = CreateOrUpdate($"{WeaponFolder}/{def.Name}.asset", fresh);
        }

        var armourAssets = new Dictionary<string, ArmourProfile>();
        foreach (ArmourDef def in Armours)
        {
            ArmourProfile fresh = ArmourProfile.CreateRuntimeProfile(
                def.Name, def.Deflect, def.Bruise, def.Bleed, def.DefenceBonus, provisional: true);
            armourAssets[def.Name] = CreateOrUpdate($"{ArmourFolder}/{def.Name}.asset", fresh);
        }

        // Created once and never overwritten, so designer edits to the tuning values survive re-running the generator.
        if (AssetDatabase.LoadAssetAtPath<TroopTuning>(TuningPath) == null)
        {
            var tuning = ScriptableObject.CreateInstance<TroopTuning>();
            AssetDatabase.CreateAsset(tuning, TuningPath);
            tuning.name = Path.GetFileNameWithoutExtension(TuningPath);
            EditorUtility.SetDirty(tuning);
        }

        int defence = CombatStatLimits.Default;

        foreach (CharacterDef character in Characters)
        {
            string soldierPath = $"{SoldierFolder}/{character.Name}.asset";
            SoldierProfile existingSoldier = AssetDatabase.LoadAssetAtPath<SoldierProfile>(soldierPath);
            Sprite sprite = existingSoldier != null ? existingSoldier.Sprite : null;

            var weapons = new List<WeaponProfile>();
            foreach (string weaponName in character.Weapons)
                weapons.Add(weaponAssets[weaponName]);

            var skills = new List<WeaponSkill>();
            foreach (var skill in character.Skills)
                skills.Add(new WeaponSkill(skill.Class, skill.Level));

            ArmourProfile armour = armourAssets[character.Armour];

            SoldierProfile soldierFresh = SoldierProfile.CreateCharacterProfile(
                character.ClassName, sprite, character.Speed, defence, character.Hp, character.Aggression,
                character.Strength, character.ChargeBonus, character.Support, weapons, skills, armour);
            SoldierProfile soldierAsset = CreateOrUpdate(soldierPath, soldierFresh);

            var composition = new List<TroopProfileCount>
            {
                new TroopProfileCount { Profile = soldierAsset, Count = character.Numbers }
            };
            TroopProfile troopFresh = TroopProfile.CreateRuntimeProfile(
                character.Morale, composition, SampleTroopRadius, character.Discipline);
            CreateOrUpdate($"{TroopFolder}/{character.Name}.asset", troopFresh);
        }

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

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        string name = Path.GetFileName(path);
        AssetDatabase.CreateFolder(parent, name);
    }
}
