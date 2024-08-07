using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RangedWeapon: MonoBehaviour
{
    [SerializeField]
    Color startColour, endColour;
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
    UnitBase unit;
    private void Start()
    {
        unit= GetComponent<UnitBase>();
        targetTemplate= GetComponentInChildren<FieldofView>();
    }
    void Update()
    {
        if(unit.State != UnitState.Idle)
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
        _timeToShoot = ReloadTime;
        UnitBase nearestTarget = null;
        float distance = float.MaxValue;
        foreach (var target in targetTemplate._targets)
        {
            if (distance > target.Value && Battle.Instance.Enemies(unit, target.Key)){
                distance = target.Value;
                nearestTarget = target.Key;
            }
        }
        if (nearestTarget != null)
        {
            DamageTarget(nearestTarget);
            _timeToShoot = 0;
        }
    }
    void DamageTarget(UnitBase target)
    {
        float damage = Random.Range(minimumDamage, maximumDamage);
        StatSystem.IDefenceStats defence = target.UnitStats as StatSystem.IDefenceStats;
        int kills = (int)(damage / defence.Defence);
        if(Notifications.RangedDamage != null)
            Notifications.RangedDamage(unit, target, kills);
        target.TakeDamage(kills);
    }
}
