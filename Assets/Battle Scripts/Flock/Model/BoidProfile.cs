using UnityEngine;

public enum BoidLifeState
{
    Active,
    Injured,
    Dead
}

[CreateAssetMenu(menuName = "Flocks/Boid Profile")]
public sealed class BoidProfile : ScriptableObject
{
    [SerializeField] Sprite _sprite;
    [SerializeField, Min(0.1f)] float _maxSpeed = 2.5f;
    [SerializeField, Min(0.1f)] float _acceleration = 8f;
    [SerializeField, Min(0f)] float _cohesion = 0.8f;
    [SerializeField, Min(0f)] float _alignment = 0.5f;
    [SerializeField, Min(0f)] float _separation = 1.8f;
    [SerializeField, Range(0f, 1f)] float _brownianMotion = 0.04f;
    [SerializeField, Min(1f)] float _maxHealth = 100f;
    [SerializeField, Range(0f, 100f)] float _aggression = 35f;
    [SerializeField, Min(0f)] float _meleeSkill = 50f;
    [SerializeField, Min(0f)] float _meleeDamage = 10f;

    public Sprite Sprite => _sprite;
    public float MaxSpeed => _maxSpeed;
    public float Acceleration => _acceleration;
    public float Cohesion => _cohesion;
    public float Alignment => _alignment;
    public float Separation => _separation;
    public float BrownianMotion => _brownianMotion;
    public float MaxHealth => _maxHealth;
    public float Aggression => _aggression;
    public float MeleeSkill => _meleeSkill;
    public float MeleeDamage => _meleeDamage;
    public static BoidProfile CreateRuntimeProfile(Sprite sprite)
    {
        var profile = CreateInstance<BoidProfile>();
        profile._sprite = sprite;
        return profile;
    }

    public void SetSpriteIfMissing(Sprite sprite)
    {
        if (_sprite == null) _sprite = sprite;
    }
}
