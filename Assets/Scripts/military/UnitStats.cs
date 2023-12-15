using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Army/UnitStats")]
public class UnitStats : ScriptableObject
{
    [SerializeField]
    float _MovementSpeed;
    public float MoveSpeed
    {
        get { return _MovementSpeed; }
    }
    [SerializeField]
    float _Speed;
    public float Speed
    {
        get { return _Speed; }
    }
    [SerializeField]
    int _WeaponSkill;
    public int WeaponSkill
    {
        get { return _WeaponSkill; }
    }
    [SerializeField]
    int _Strength;
    public int AttackStrength
    {
        get { return _Strength; }
    }
    [SerializeField]
    int _Defence;
    public int Defence
    {
        get { return _Defence; }
    }
}
