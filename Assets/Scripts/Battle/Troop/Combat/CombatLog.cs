using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public enum CombatLogKind { Clash, Shot }

// One soldier named in a combat log entry.
public struct CombatLogSoldier
{
    public string Troop;
    public int TeamId;
    public Color Colour;
    public int SoldierId;

    public static CombatLogSoldier Of(Troop troop, SoldierAgentState soldier) => new()
    {
        Troop = troop == null ? "?" : troop.DisplayName,
        TeamId = troop != null ? troop.TeamId : -1,
        Colour = troop != null ? troop.ArmyColor : Color.white,
        SoldierId = soldier != null ? soldier.Id : -1,
    };

    public override string ToString() => SoldierId >= 0 ? $"{Troop} #{SoldierId}" : Troop;
}

// The parts of one soldier's Combat Power in a clash (spec §4.5).
public struct CombatPowerParts
{
    public int Skill;
    public int Charge;    // ChargeStat + WeaponCharge + AssaultCharge, while charging; 0 otherwise
    public int ChargeStat;     // the soldier's Charge stat (while charging)
    public int WeaponCharge;   // the melee weapon's charge bonus (while charging)
    public int AssaultCharge;  // AssaultChargeBonus (while charging during Assault)
    public string Weapon;      // the melee weapon's name
    public int Support;
    public bool Charging;
    public bool Assault;
    public bool Flanked;  // fighting an enemy on its troop's flank or rear: no support
    public bool Stunned;  // stunned (SU-107): deals no damage if it wins (SU-111)
    public int Total => Skill + Charge + Support;
}

// What one hit did (spec §4.6): raw damage, the armour roll, and the HP the soldier lost.
public struct CombatLogDamage
{
    public bool Dealt;           // false: a miss, or no damage (a fleeing winner, Q-37)
    public string NoDamageReason;
    public int Strength;         // melee only
    public int WeaponDamage;
    public int Domination;       // melee only
    public bool LanceDoubled;    // Couched Lance while charging
    public int Raw;
    public int Deflected, Bruised, Bled, Overflow;
    public int AppliedHp;        // after the HP clamp
    public int HpLeft;
    public bool Killed, Stunned;
}

public struct CombatLogEntry
{
    public CombatLogKind Kind;
    public float Time;                 // battle seconds (TroopCombatWorld.BattleTime)
    // Clash: A (the charger, the earlier charger, or the lower id, Q-28) and B. Shot: the shooter and the target.
    public CombatLogSoldier Attacker, Defender;
    // Clash
    public CombatPowerParts AttackerPower, DefenderPower;
    public float ChanceA;              // P_A, 0-1
    public float Roll;                 // R, 0-100
    public bool AttackerWins;
    // Shot
    public string Weapon;
    public float HitChance;            // 0-1
    public float HitRoll;              // 0-1, a hit when below HitChance
    public bool Hit;
    // The hit: winner on loser, or the shot on its target.
    public CombatLogDamage Damage;

    public CombatLogSoldier Winner => Kind == CombatLogKind.Shot || AttackerWins ? Attacker : Defender;
    public CombatLogSoldier Loser => Kind == CombatLogKind.Shot || AttackerWins ? Defender : Attacker;
}

// Battle-wide record of every clash and ranged attack (combat log panel, CombatLogHud). Keeps the newest Capacity
// entries. Cleared when a battle starts (TroopCombatWorld.Initialize) and when play mode starts.
public static class CombatLog
{
    public const int Capacity = 600;
    static readonly List<CombatLogEntry> _entries = new();
    static int _start;

    public static event Action<CombatLogEntry> Logged;
    // Every entry ever recorded this battle, including ones dropped from the buffer.
    public static int TotalRecorded { get; private set; }
    public static int Count => _entries.Count;

    // Oldest first.
    public static CombatLogEntry Get(int index) => _entries[(_start + index) % _entries.Count];

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Clear();
        Logged = null;
    }

    public static void Clear()
    {
        _entries.Clear();
        _start = 0;
        TotalRecorded = 0;
    }

    internal static void Record(in CombatLogEntry entry)
    {
        if (_entries.Count < Capacity) _entries.Add(entry);
        else
        {
            _entries[_start] = entry;
            _start = (_start + 1) % Capacity;
        }
        TotalRecorded++;
        Logged?.Invoke(entry);
    }

    internal static float Now => TroopCombatWorld.Current != null ? TroopCombatWorld.Current.BattleTime : 0f;

    // --- Text ---

    // One short line per step, so it reads down the panel: who fought; each side's Combat Power (A and B) with the charge
    // bonus broken into the Charge stat, the weapon's charge bonus and Assault; the chance, roll and winner; the damage
    // and what it did. A shot: shooter and target, the hit roll, then the damage.
    public static string Format(in CombatLogEntry entry)
    {
        var text = new StringBuilder(240);
        text.Append(FormatTime(entry.Time)).Append("  ");
        if (entry.Kind == CombatLogKind.Clash)
        {
            text.Append("Clash  A ").Append(entry.Attacker).Append(Tags(entry.AttackerPower))
                .Append("  vs  B ").Append(entry.Defender).Append(Tags(entry.DefenderPower));
            text.Append("\n   A: ").Append(Power(entry.AttackerPower));
            text.Append("\n   B: ").Append(Power(entry.DefenderPower));
            text.Append("\n   P(A) ").Append(Mathf.RoundToInt(entry.ChanceA * 100f)).Append("%, roll ")
                .Append(Mathf.RoundToInt(entry.Roll)).Append(" → ").Append(entry.AttackerWins ? "A" : "B").Append(" wins");
            if (entry.Damage.Domination > 0) text.Append(", Domination +").Append(entry.Damage.Domination);
        }
        else
        {
            text.Append("Shot  ").Append(entry.Attacker).Append(" → ").Append(entry.Defender);
            text.Append("\n   ").Append(string.IsNullOrEmpty(entry.Weapon) ? "Ranged" : entry.Weapon).Append(": hit ")
                .Append(Mathf.RoundToInt(entry.HitChance * 100f)).Append("%, roll ")
                .Append(Mathf.RoundToInt(entry.HitRoll * 100f)).Append(" → ").Append(entry.Hit ? "hit" : "miss");
        }
        if (entry.Kind == CombatLogKind.Clash || entry.Hit) DamageText(text, entry);
        return text.ToString();
    }

    static string FormatTime(float seconds)
    {
        int whole = Mathf.Max(0, Mathf.FloorToInt(seconds));
        return $"{whole / 60}:{whole % 60:00}";
    }

    static string Tags(in CombatPowerParts power)
    {
        string tags = string.Empty;
        if (power.Charging) tags += power.Assault ? " (charging, Assault)" : " (charging)";
        else if (power.Assault) tags += " (Assault)";
        if (power.Flanked) tags += " (flanked)";
        if (power.Stunned) tags += " (stunned)";
        return tags;
    }

    // "9 skill + 24 charge (11 Charge + 8 Lance + 5 Assault) + 6 support = 39"
    static string Power(in CombatPowerParts power)
    {
        var text = new StringBuilder();
        text.Append(power.Skill).Append(" skill");
        if (power.Charge != 0)
        {
            text.Append(" + ").Append(power.Charge).Append(" charge (").Append(power.ChargeStat).Append(" Charge");
            if (power.WeaponCharge != 0)
                text.Append(" + ").Append(power.WeaponCharge).Append(' ').Append(string.IsNullOrEmpty(power.Weapon) ? "weapon" : power.Weapon);
            if (power.AssaultCharge != 0) text.Append(" + ").Append(power.AssaultCharge).Append(" Assault");
            text.Append(')');
        }
        if (power.Support != 0) text.Append(" + ").Append(power.Support).Append(" support");
        text.Append(" = ").Append(power.Total);
        return text.ToString();
    }

    static void DamageText(StringBuilder text, in CombatLogEntry entry)
    {
        CombatLogDamage d = entry.Damage;
        text.Append("\n   ");
        if (!d.Dealt)
        {
            text.Append("No damage");
            if (!string.IsNullOrEmpty(d.NoDamageReason)) text.Append(" (").Append(d.NoDamageReason).Append(')');
            return;
        }
        text.Append("Damage ");
        if (entry.Kind == CombatLogKind.Clash)
        {
            text.Append(d.Strength).Append(" str + ").Append(d.WeaponDamage).Append(" weapon");
            if (d.Domination > 0) text.Append(" + ").Append(d.Domination).Append(" dom");
            if (d.LanceDoubled) text.Append(", x2 lance");
            text.Append(" = ").Append(d.Raw);
        }
        else text.Append(d.Raw);
        text.Append(" → ").Append(d.Deflected).Append(" deflect, ").Append(d.Bruised).Append(" bruise, ")
            .Append(d.Bled).Append(" bleed");
        if (d.Overflow > 0) text.Append(", ").Append(d.Overflow).Append(" overflow");
        text.Append("\n   ").Append(entry.Loser).Append(" loses ").Append(d.AppliedHp).Append(" HP");
        if (d.Killed) text.Append(": killed");
        else if (d.Stunned) text.Append(": stunned (").Append(d.HpLeft).Append(" HP)");
        else text.Append(", ").Append(d.HpLeft).Append(" HP left");
    }
}
