using System.Collections;
using System.Collections.Generic;
using RangedWeapons;
using Shooting;
using UnityEngine;

public class RangedWeapon: MonoBehaviour
{
    public event System.Action<IUnit, Vector2, Transform, float> ShootAt;
    [SerializeField]
    Color startColour, endColour;
    public IProjectile _projectile;
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
    [SerializeField, Range(1, 20)]
    public float accuracy;
    FieldofView targetTemplate;
    float _timeToShoot = 0;
    IUnit unit;
    
    IRangedTargeter target;
    public Transform CurrentTarget
    {
        get
        {
            return target.Target;
        }
    }
    public void Setup(StatSystem.Refactor.PC_Stats character)
    {
        _projectile = character._rangedWeapon._projectile;
        WeaponRange = character._rangedWeapon.range;
        ReloadTime =character._rangedWeapon.ReloadTimeBase;
        minimumDamage = character._rangedWeapon.MinDamage;
        maximumDamage = character._rangedWeapon.MaxDamage;
        accuracy = character.Accuracy;
    }
    private void Start()
    {
        unit= GetComponentInParent<IUnit>();
        ReloadTime *= Mathf.Lerp(1, 0.5f, unit.Stats.ShootSpeed / 20f); 
        targetTemplate= GetComponentInChildren<FieldofView>();
        accuracy = Mathf.Clamp(accuracy, 1, 20);
        target = GetComponentInChildren<IRangedTargeter>();
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
        _timeToShoot = 0;
        if(target.ValidTargets.Count == 0 || target.Target == null) return;
        Debug.Log($"Shooting at {target.Target.name}");
        ShootAt?.Invoke(unit, target.Target.position, target.Target, accuracy);
        //Shoot(projectile, Random.Range(minimumDamage, maximumDamage) * 50, target.Target.transform);
    }
    public float ShootPower
    {
        get => Random.Range(minimumDamage, maximumDamage) * 5;
    }
    private void OnDrawGizmos()
    {
        if (target== null || target.ValidTargets.Count == 0 || target.Target == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.parent.position, target.Target.position);
        Gizmos.DrawWireSphere(target.Target.position, _projectile.Inaccuracy(Vector2.Distance(transform.parent.position, target.Target.position), accuracy));
    }
}
