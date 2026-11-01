using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

// A troop before battle (spec §1.1, §1.2): the characters that make it up. One character is an ordinary troop; two or
// more are a composite troop. The troop stats are resolved from the characters with CompositeTroopResolver:
//   Move = min(Speed_i), BaseMorale = max(Morale_i), Discipline = max(Discipline_i), soldiers = Σ Numbers_i.
// Each character spawns its own Numbers of soldiers.
[CreateAssetMenu(menuName = "Troops/Troop Profile")]
public sealed class TroopProfile : ScriptableObject
{
    [SerializeField, Tooltip("The characters in this troop. The first one names the troop and gives its portrait.")]
    List<CharacterProfile> _characters = new();
    [SerializeField, Min(0.1f), FormerlySerializedAs("_flockRadius"),
     Tooltip("Troop radius. The natural line length is L_nat = NaturalLineRadiusScale x TroopRadius (spec §3.3).")]
    float _troopRadius = 16.5f;

    readonly List<TroopStatLine> _lines = new();

    public IReadOnlyList<CharacterProfile> Characters => _characters;
    public float TroopRadius => _troopRadius;

    // The first character, or null when the list is empty.
    public CharacterProfile FirstCharacter
    {
        get
        {
            foreach (var character in _characters)
                if (character != null) return character;
            return null;
        }
    }

    // The troop's name and portrait are its first character's.
    public string DisplayName => FirstCharacter != null ? FirstCharacter.CharacterName : name;
    public Sprite Portrait => FirstCharacter != null ? FirstCharacter.Portrait : null;

    public ResolvedTroopStats Resolved
    {
        get
        {
            _lines.Clear();
            foreach (var character in _characters)
                if (character != null) _lines.Add(character.StatLine);
            return CompositeTroopResolver.Resolve(_lines);
        }
    }

    public int Move => Resolved.Move;
    public int BaseMorale => Resolved.BaseMorale;
    public int Discipline => Resolved.Discipline;
    public int SoldierCount => Resolved.InitialSoldierCount;

    public static TroopProfile CreateRuntimeProfile(IEnumerable<CharacterProfile> characters, float troopRadius)
    {
        var profile = CreateInstance<TroopProfile>();
        profile._characters = new List<CharacterProfile>(characters);
        profile._troopRadius = Mathf.Max(0.1f, troopRadius);
        return profile;
    }

    // Pairs troops into one composite troop before battle (spec §1.2): all their characters in order. Radius takes the
    // largest part value (Questions.md Q-53, working assumption).
    public static TroopProfile CreateComposite(IReadOnlyList<TroopProfile> parts)
    {
        var characters = new List<CharacterProfile>();
        float radius = 0f;
        foreach (var part in parts)
        {
            if (part == null) continue;
            radius = Mathf.Max(radius, part.TroopRadius);
            foreach (var character in part.Characters)
                if (character != null) characters.Add(character);
        }
        return CreateRuntimeProfile(characters, radius);
    }
}
