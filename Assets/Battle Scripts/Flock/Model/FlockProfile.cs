using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class FlockProfileCount
{
    public BoidProfile Profile;
    [Min(0)] public int Count = 1;
}

[CreateAssetMenu(menuName = "Flocks/Flock Profile")]
public sealed class FlockProfile : ScriptableObject
{
    [SerializeField, Min(0f)] float _morale = 100f;
    [SerializeField] List<FlockProfileCount> _composition = new();
    [SerializeField, Min(0.1f)] float _flockRadius = 102f * 0.052f;
    [SerializeField, Min(0.5f)] float _lineWidth = 7f;

    public float Morale => _morale;
    public IReadOnlyList<FlockProfileCount> Composition => _composition;
    public float FlockRadius => _flockRadius;
    public float LineWidth => _lineWidth;

    public static FlockProfile CreateRuntimeProfile(
        float morale,
        IEnumerable<FlockProfileCount> composition,
        float flockRadius,
        float lineWidth)
    {
        var profile = CreateInstance<FlockProfile>();
        profile._morale = Mathf.Max(0f, morale);
        profile._composition = new List<FlockProfileCount>(composition);
        profile._flockRadius = Mathf.Max(0.1f, flockRadius);
        profile._lineWidth = Mathf.Max(0.5f, lineWidth);
        return profile;
    }
}
