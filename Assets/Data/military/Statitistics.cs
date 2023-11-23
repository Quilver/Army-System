using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Stats
{
    public enum Stats
    {
        MovementSpeed,
        Leadership,
        Hitpoints,
        Endurance,
        Armour,
        Altsave,
        Melee_skill,
        Accuracy,
        Strength,
        Attacks
    }
    public struct Modifier
    {
        public Stats stat;
        public int magnitude;
    }
    public enum SpecialRules
    {
        Armour_piercing,
        Death_blow,
        Shield,
        Strike_first,
        Poison,
        Caster,
        Leader,
        Guardian,
        Terror,
        Unbreakable, 
        ChargeBonus
    }
    public enum RangedAttackType
    {
        Shot,
        Bombardment
    }
    public struct Rule
    {
        public SpecialRules rule;
        public int magnitude;
    };
    public enum MeleeWeapons
    {
        Hand_weapon,
        Sword,
        Spear,
        Lance,
        Shield,
        Great_weapon,
        Mace,
        Dagger
    }
    public enum RangedWeapon
    {
        Javelin,
        Crossbow,
        Lowbow,
        Pistol,
        Rifle
    }
    public enum EquipmentSlots
    {
        Mainhand,
        Offhand,
        Helmet,
        Armour,
        Ring,
        None
    }
    public struct UnitStats
    {
        string name;
        Dictionary<Stats, int> statistcs, derivedStats;
        //MeleeWeaponStats mainHand, offHand;
        Helmet helmet;
        Armour armour;
        Ring[] ring;
        List<Rule> rules;
    }
    public abstract class Item
    {
        protected EquipmentSlots slot;
        public string name;
        public List<Rule> rules;
        public List<Modifier> modifiers;
        public Item(string name)
        {
            this.name = name;
            rules = new List<Rule>();
            modifiers = new List<Modifier>();
        }
        public void add(Modifier mod)
        {
            modifiers.Add(mod);
        }
        public void add(Rule rule)
        {
            rules.Add(rule);
        }
        public abstract EquipmentSlots getSlot();
    };
    public abstract class Weapon : Item
    {
        public bool twoHanded, offHand;
        public Weapon(string name, bool twoHands, bool offhand) : base(name)
        {
            slot = EquipmentSlots.Mainhand;
            twoHanded = twoHands;
            offHand = offhand;
        }
        public override EquipmentSlots getSlot()
        {
            if (offHand)
            {
                return EquipmentSlots.Offhand;
            }
           else
            {
                return EquipmentSlots.Mainhand;
            }
        }
    };
    public class MeleeWeapon : Weapon
    {
        public MeleeWeapons type;
        public MeleeWeapon(string name, bool twoHands, bool offhand, MeleeWeapons type) : base(name, twoHands, offhand)
        {
            this.type = type;
        }
    };
    public class RangeWeapon : Weapon
    {
        public RangedWeapon tag;
        public RangedAttackType type;
        public int range, strength;
        public float reloadSpeed;
        public RangeWeapon
            (string name, bool twoHands, bool offhand, RangedWeapon tag, RangedAttackType type, int range, int strength, float reloadTime = 3)
            : base(name, twoHands, offhand)
        {
            this.tag = tag;
            this.type = type;
            this.range = range;
            this.strength = strength;
            reloadSpeed = reloadTime;
        }
    };
    public class Helmet: Item
    {
        public Helmet(string name) : base(name)
        {
            slot = EquipmentSlots.Helmet;
        }
        public override EquipmentSlots getSlot()
        {
            return slot;
        }
    };
    public class Armour : Item
    {
        public Armour(string name) : base(name)
        {
            slot = EquipmentSlots.Armour;
        }
        public override EquipmentSlots getSlot()
        {
            return slot;
        }
    };
    public class Ring : Item
    {
        public Ring(string name) : base(name)
        {
            slot = EquipmentSlots.Armour;
        }
        public override EquipmentSlots getSlot()
        {
            return slot;
        }
    };
    public class UnitLoadOut : Item
    {
        string prefabName;
        UnitType size;
        Dictionary<EquipmentSlots, Item> equipment;
        Dictionary<Stats, int> baseStats, derivedStats;
        public UnitLoadOut(string name, string prefabName, UnitType size, Dictionary<Stats, int> stats) : base(name)
        {
            slot = EquipmentSlots.None;
            this.size = size;
            this.prefabName = prefabName;
            baseStats = stats;
            initDerStats();
        }
        void initDerStats()
        {
            derivedStats = new Dictionary<Stats, int>();
            foreach(var stat in baseStats)
            {
                derivedStats.Add(stat.Key, stat.Value);
            }
        }
        public void add(Item item)
        {

        }
        public int getStat(Stats stat)
        {
            return 0;
        }
        public override EquipmentSlots getSlot()
        {
            return slot;
        }
    }
    public class LoadClass
    {
        public LoadClass() {
        }
        Dictionary<string, MeleeWeapon> initialisingMeleeWeapons()
        {
            Dictionary<string, MeleeWeapon> weapons = new Dictionary<string, MeleeWeapon>();
            //modifiers and special rules
            Modifier weaponBonus = new Modifier
            {
                stat = Stats.Melee_skill,
                magnitude = 1
            };
            Modifier armourBonus = new Modifier
            {
                stat = Stats.Armour,
                magnitude = 1
            };
            Modifier strengthBonusLow = new Modifier
            {
                stat = Stats.Strength,
                magnitude = 1
            };
            Modifier strengthBonusHigh = new Modifier
            {
                stat = Stats.Strength,
                magnitude = 2
            };
            Modifier attackBonus = new Modifier
            {
                stat = Stats.Attacks,
                magnitude = 1
            };
            Rule chargeBonus = new Rule
            {
                rule = SpecialRules.ChargeBonus,
                magnitude = 2
            };
            Rule slowStrike= new Rule
            {
                rule = SpecialRules.Strike_first,
                magnitude = -1
            };
            Rule armourPiercing = new Rule
            {
                rule = SpecialRules.Armour_piercing,
                magnitude = 4
            };
            //hand weapon
            MeleeWeapon melee = new MeleeWeapon("Hand weapon", false, false, MeleeWeapons.Hand_weapon);
            weapons.Add(melee.name, melee);
            //sword
            melee = new MeleeWeapon("Sword", false, false, MeleeWeapons.Sword);
            melee.add(weaponBonus);
            weapons.Add(melee.name, melee);
            //lance
            melee = new MeleeWeapon("Lance", false, false, MeleeWeapons.Lance);
            melee.add(chargeBonus);
            weapons.Add(melee.name, melee);
            //dagger
            melee = new MeleeWeapon("Dagger", false, true, MeleeWeapons.Dagger);
            melee.add(attackBonus);
            weapons.Add(melee.name, melee);
            //mace
            melee = new MeleeWeapon("Mace", true, false, MeleeWeapons.Mace);
            melee.add(strengthBonusLow);
            melee.add(armourPiercing);
            weapons.Add(melee.name, melee);
            //great weapon
            melee = new MeleeWeapon("Great weapon", true, false, MeleeWeapons.Great_weapon);
            melee.add(strengthBonusHigh);
            melee.add(slowStrike);
            weapons.Add(melee.name, melee);
            //shield
            melee = new MeleeWeapon("Shield", false, true, MeleeWeapons.Shield);
            melee.add(weaponBonus);
            melee.add(armourBonus);
            weapons.Add(melee.name, melee);
            return weapons;
        }
        Dictionary<string, RangeWeapon> initialisingRangedWeapons()
        {
            Dictionary<string, RangeWeapon> weapons = new Dictionary<string, RangeWeapon>();
            Modifier accuracyCost = new Modifier
            {
                stat = Stats.Accuracy,
                magnitude = -2
            };
            Rule armourPiercing = new Rule
            {
                rule = SpecialRules.Armour_piercing,
                magnitude = 2
            };
            RangeWeapon weapon;
            //pistol
            weapon = new RangeWeapon("Pistol", false, true, RangedWeapon.Pistol, RangedAttackType.Shot, 5, 3);
            weapon.add(armourPiercing);
            weapons.Add(weapon.name, weapon);
            //javelin
            weapon = new RangeWeapon("Javelin", false, true, RangedWeapon.Javelin, RangedAttackType.Shot, 5, 4);
            weapons.Add(weapon.name, weapon);
            //crosswbow
            weapon = new RangeWeapon("Crossbow", false, false, RangedWeapon.Crossbow, RangedAttackType.Bombardment, 20, 4);
            weapons.Add(weapon.name, weapon);
            //longbow
            weapon = new RangeWeapon("Longbow", true, false, RangedWeapon.Lowbow, RangedAttackType.Bombardment, 55, 6);
            weapon.add(accuracyCost);
            weapons.Add(weapon.name, weapon);
            //rifle
            weapon = new RangeWeapon("Rifle", false, true, RangedWeapon.Rifle, RangedAttackType.Shot, 20, 5);
            weapon.add(armourPiercing);
            weapons.Add(weapon.name, weapon);
            return weapons;
        }
        Dictionary<string, Helmet> initialisingHelmet()
        {
            Dictionary<string, Helmet> helmets = new Dictionary<string, Helmet>();
            Modifier armourBonus = new Modifier
            {
                stat = Stats.Armour,
                magnitude = 2
            };
            Helmet helmet = new Helmet("Helmet");
            helmet.add(armourBonus);
            helmets.Add(helmet.name, helmet);
            return helmets;
        }
        Dictionary<string, Armour> initialisingArmour()
        {
            Dictionary<string, Armour> armours = new Dictionary<string, Armour>();
            Modifier armourBonus = new Modifier
            {
                stat = Stats.Armour,
                magnitude = 2
            };
            //no armour
            Armour armour = new Armour("No armour");
            armourBonus.magnitude = 0;
            armour.add(armourBonus);
            armours.Add(armour.name, armour);
            //light armour
            armour = new Armour("Light armour");
            armourBonus.magnitude = 1;
            armour.add(armourBonus);
            armours.Add(armour.name, armour);
            //medium armour
            armour = new Armour("Medium armour");
            armourBonus.magnitude = 2;
            armour.add(armourBonus);
            armours.Add(armour.name, armour);
            //heavy armour
            armour = new Armour("Heavy armour");
            armourBonus.magnitude = 4;
            armour.add(armourBonus);
            armours.Add(armour.name, armour);
            //full armour
            armour = new Armour("Full armour");
            armourBonus.magnitude = 6;
            armour.add(armourBonus);
            armours.Add(armour.name, armour);
            return armours;
        }
        Dictionary<string, Ring> initialisingRing()
        {
            Dictionary<string, Ring> rings = new Dictionary<string, Ring>();
            //modifiers and special rules
            /*Modifier armourBonus = new Modifier
            {
                stat = Stats.Armour,
                magnitude = 1
            };
            Modifier strengthBonus = new Modifier
            {
                stat = Stats.Strength,
                magnitude = 2
            };*/
            Modifier attackBonus = new Modifier
            {
                stat = Stats.Attacks,
                magnitude = 2
            };
            Modifier healthBonus = new Modifier
            {
                stat = Stats.Hitpoints,
                magnitude = 1
            };
            Rule armourPiercing = new Rule
            {
                rule = SpecialRules.Armour_piercing,
                magnitude = 4
            };

            //no rings
            Ring ring = new Ring("Empty ring");
            rings.Add(ring.name, ring);
            //life rings
            ring = new Ring("Life ring");
            ring.add(healthBonus);
            rings.Add(ring.name, ring);
            //attack rings
            ring = new Ring("Striking ring");
            ring.add(attackBonus);
            rings.Add(ring.name, ring);
            //armour piercing rings
            ring = new Ring("Armour piercing ring");
            ring.add(armourPiercing);
            rings.Add(ring.name, ring);
            return rings;
        }
        Dictionary<string, ModelType> initialiseUnits()
        {
            Dictionary<string, ModelType> units = new Dictionary<string, ModelType>();
            CombatStats stats = new CombatStats
            {
                leadership = 0,
                armour = 2,
                weaponSkill = 1,
                strength = 3,
                toughness = 3,
                wounds = 1
            };
            ModelType skeleton = new ModelType
            {
                name = "skeleton",
                prefabName = "Skeleton/Skeleton",
                character = false,
                caster = false,
                unitType = UnitType.Infantry,
                movementSpeed = 4,
                combatStats = stats
            };
            units.Add(skeleton.name, skeleton);
            stats = new CombatStats
            {
                leadership = 7,
                armour = 4,
                weaponSkill = 3,
                strength = 3,
                toughness = 3,
                wounds = 1
            };
            ModelType footman = new ModelType
            {
                name = "footman",
                prefabName = "Footman/Footman",
                character = false,
                caster = false,
                unitType = UnitType.Infantry,
                movementSpeed = 6,
                combatStats = stats
            };
            units.Add(footman.name, footman);
            return units;
        }
        Dictionary<string, UnitLoadOut> initialiseUnitsTypes()
        {
            Dictionary<string, UnitLoadOut> units = new Dictionary<string, UnitLoadOut>();
            return units;
        }
        Dictionary<Stats, int> initialiseStats()
        {
            Dictionary<Stats, int> stats = new Dictionary<Stats, int>();
            foreach (Stats stat in System.Enum.GetValues( typeof(Stats) ) )
            {
                stats.Add(stat, 4);
            }
            stats[Stats.Leadership] = 9;
            stats[Stats.MovementSpeed] = 5;
            stats[Stats.Accuracy] = 3;
            stats[Stats.Altsave] = 0;
            stats[Stats.Armour] = 0;
            stats[Stats.Attacks] = 1;
            stats[Stats.Hitpoints] = 5;
            return stats;
        }
        Dictionary<Stats, int> derivedStats(Dictionary<Stats, int> initialStats)
        {
            Dictionary<Stats, int> stats = new Dictionary<Stats, int>();
            //foreach(Stats stat in initialiseStats.)
            foreach (Stats entry in initialStats.Keys)
            {
                stats.Add(entry, initialStats[entry]);
            }
            return stats;
        }
    }
}
