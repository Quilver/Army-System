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

[CreateAssetMenu(menuName = "Troops/Troop Profile")]
public sealed class TroopProfile : ScriptableObject
{
    [SerializeField, Min(0f)] float _morale = 100f;
    [SerializeField] List<TroopProfileCount> _composition = new();
    [SerializeField, Min(0.1f), FormerlySerializedAs("_flockRadius")] float _troopRadius = 16.5f;
    [SerializeField, Min(0.5f)] float _lineWidth = 22f;

    public float Morale => _morale;
    public IReadOnlyList<TroopProfileCount> Composition => _composition;
    public float TroopRadius => _troopRadius;
    public float LineWidth => _lineWidth;

    public static TroopProfile CreateRuntimeProfile(
        float morale,
        IEnumerable<TroopProfileCount> composition,
        float troopRadius,
        float lineWidth)
    {
        var profile = CreateInstance<TroopProfile>();
        profile._morale = Mathf.Max(0f, morale);
        profile._composition = new List<TroopProfileCount>(composition);
        profile._troopRadius = Mathf.Max(0.1f, troopRadius);
        profile._lineWidth = Mathf.Max(0.5f, lineWidth);
        return profile;
    }
}
