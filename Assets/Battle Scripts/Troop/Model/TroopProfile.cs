using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public sealed class TroopProfileCount
{
    public SoldierProfile Profile;
    [Min(0)] public int Count = 1;
}

// A troop's stats and composition (spec §1.4). Move is derived from the soldiers, not stored.
// BaseMorale and Discipline are integers 1-20, default 5 (see CombatStatLimits).
[CreateAssetMenu(menuName = "Troops/Troop Profile")]
public sealed class TroopProfile : ScriptableObject
{
    // Per-soldier morale (spec §5.1). Renamed from the old absolute morale value (10-100). The old serialized value is
    // not carried over, so existing assets reset to 5 until they are set in the Editor.
    [SerializeField, Range(1, 20), Tooltip("Morale per soldier. EffectiveMorale = BaseMorale x living soldiers.")]
    int _baseMorale = CombatStatLimits.Default;
    [SerializeField, Range(1, 20), Tooltip("Number of nearest friendly soldiers that may follow a charger (spec §4.4).")]
    int _discipline = CombatStatLimits.Default;
    [SerializeField] List<TroopProfileCount> _composition = new();
    [SerializeField, Min(0.1f), FormerlySerializedAs("_flockRadius"),
     Tooltip("Troop radius. The natural line length is L_nat = NaturalLineRadiusScale x TroopRadius (spec §3.3).")]
    float _troopRadius = 16.5f;

    public int BaseMorale => _baseMorale;
    public int Discipline => _discipline;
    public IReadOnlyList<TroopProfileCount> Composition => _composition;
    public float TroopRadius => _troopRadius;

    // Lowest soldier Speed in the composition (spec §1.4). Zero when there are no soldiers.
    public int Move
    {
        get
        {
            bool found = false;
            int move = 0;
            foreach (var entry in _composition)
            {
                if (entry == null || entry.Profile == null || entry.Count <= 0) continue;
                move = found ? Mathf.Min(move, entry.Profile.Speed) : entry.Profile.Speed;
                found = true;
            }
            return move;
        }
    }

    public int SoldierCount
    {
        get
        {
            int total = 0;
            foreach (var entry in _composition)
                if (entry != null && entry.Profile != null) total += Mathf.Max(0, entry.Count);
            return total;
        }
    }

    public static TroopProfile CreateRuntimeProfile(
        int baseMorale,
        IEnumerable<TroopProfileCount> composition,
        float troopRadius,
        int discipline = CombatStatLimits.Default)
    {
        var profile = CreateInstance<TroopProfile>();
        profile._baseMorale = CombatStatLimits.Clamp(baseMorale);
        profile._discipline = CombatStatLimits.Clamp(discipline);
        profile._composition = new List<TroopProfileCount>(composition);
        profile._troopRadius = Mathf.Max(0.1f, troopRadius);
        return profile;
    }

    // Pairs parts into one composite troop before battle (spec §1.2). The battle receives the result as an ordinary
    // profile. Radius takes the largest part value (Questions.md Q-53, working assumption).
    public static TroopProfile CreateComposite(IReadOnlyList<TroopProfile> parts)
    {
        var lines = new List<TroopStatLine>(parts.Count);
        var composition = new List<TroopProfileCount>();
        float radius = 0f;

        foreach (var part in parts)
        {
            lines.Add(new TroopStatLine(part.Move, part.BaseMorale, part.Discipline, part.SoldierCount));
            radius = Mathf.Max(radius, part.TroopRadius);
            foreach (var entry in part.Composition)
                composition.Add(new TroopProfileCount { Profile = entry.Profile, Count = entry.Count });
        }

        ResolvedTroopStats resolved = CompositeTroopResolver.Resolve(lines);
        return CreateRuntimeProfile(resolved.BaseMorale, composition, radius, resolved.Discipline);
    }
}
