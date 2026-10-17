using UnityEngine;

public enum SoldierLifeState
{
    Active,
    Injured,
    Dead
}

[CreateAssetMenu(menuName = "Troops/Soldier Profile")]
public sealed class SoldierProfile : ScriptableObject
{
    [SerializeField] Sprite _sprite;
    [SerializeField, Min(0.1f)] float _maxSpeed = 2.5f;
    [SerializeField, Min(0.1f)] float _acceleration = 8f;
    [SerializeField, Min(0f)] float _alignment = 0.5f;
    [SerializeField, Min(0f)] float _separation = 1.8f;
    [SerializeField, Range(0f, 1f)] float _brownianMotion = 0.04f;
    [SerializeField, Min(1f)] float _maxHealth = 100f;
    // Expected melee attacks per TroopConstants.PerMeleeTime seconds while engaged (5 = one attack every 6 s).
    [SerializeField, Range(1f, 20f)] float _aggression = 5f;
    // 0 = attacks are independent; TroopConstants.MaxDiscipline = all soldiers in the troop strike in unison.
    [SerializeField, Range(0f, 20f)] float _discipline = 5f;
    [SerializeField, Min(0f)] float _meleeSkill = 50f;
    [SerializeField, Min(0f)] float _meleeDamage = 10f;

    public Sprite Sprite => _sprite;
    public float MaxSpeed => _maxSpeed;
    public float Acceleration => _acceleration;
    public float Alignment => _alignment;
    public float Separation => _separation;
    public float BrownianMotion => _brownianMotion;
    public float MaxHealth => _maxHealth;
    public float Aggression => _aggression;
    public float Discipline => _discipline;
    public float MeleeSkill => _meleeSkill;
    public float MeleeDamage => _meleeDamage;
    public static SoldierProfile CreateRuntimeProfile(Sprite sprite)
    {
        var profile = CreateInstance<SoldierProfile>();
        profile._sprite = sprite;
        return profile;
    }

    // Test-only variant with the combat and movement values that benchmark cases vary.
    public static SoldierProfile CreateRuntimeProfile(Sprite sprite, float aggression, float meleeSkill, float discipline, float maxSpeed)
    {
        var profile = CreateInstance<SoldierProfile>();
        profile._sprite = sprite;
        profile._aggression = Mathf.Clamp(aggression, 1f, 20f);
        profile._meleeSkill = Mathf.Max(0f, meleeSkill);
        profile._discipline = Mathf.Clamp(discipline, 0f, 20f);
        profile._maxSpeed = Mathf.Max(0.1f, maxSpeed);
        return profile;
    }

    public void SetSpriteIfMissing(Sprite sprite)
    {
        if (_sprite == null) _sprite = sprite;
    }
}
