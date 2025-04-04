using System.Collections;
using System.Collections.Generic;
using SoftBody;
using UnityEngine;

public class RangedWeapon: MonoBehaviour
{
    [SerializeField]
    Color startColour, endColour;
    [SerializeField]
    GameObject projectile;
    public Color CurrentColour
    {
        get
        {
            return Color.Lerp(startColour, endColour, _timeToShoot / ReloadTime);
        }
    }
    [SerializeField, Range(5, 40)]
    float WeaponRange;
    public float Range { get { return WeaponRange; } }
    [SerializeField, Range(3, 30)]
    float ReloadTime;
    [SerializeField, Range(0, 5)]
    float minimumDamage;
    [SerializeField, Range(3, 15)] 
    float maximumDamage;
    FieldofView targetTemplate;
    float _timeToShoot = 0;
    UnitTemplate unit;
    UnitFormation formation;
    private void Start()
    {
        unit= GetComponentInParent<UnitTemplate>();
        targetTemplate= GetComponentInChildren<FieldofView>();
        formation = GetComponentInParent<UnitFormation>();
    }
    void Update()
    {
        if(unit.unitState != UnitState.Idle)
        {
            _timeToShoot = 0;
            return;
        }
        _timeToShoot += Time.deltaTime;
        if(_timeToShoot > ReloadTime)
        {
            Shoot();    
        }
    }
    void Shoot()
    {
        _timeToShoot = 0;
        if(targetTemplate._targets.Count == 0) return;
        foreach (var model in formation.models)
        {
            model.Shoot(projectile, Random.Range(minimumDamage, maximumDamage) * 50, targetTemplate.NearestUnit.transform);
        }
    }
    
}
