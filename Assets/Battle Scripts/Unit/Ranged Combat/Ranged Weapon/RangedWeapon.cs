using System.Collections;
using System.Collections.Generic;
using RangedWeapons;
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
    [SerializeReference, SubclassSelector]
    RangedWeapons.Target.ITargetSelection target;
    public Transform CurrentTarget=>target.CurrentTarget.target;
    public List<RangedWeapons.Target.ITargetEnemy.Target> Targets=>target.ValidTargets;
    public void Setup(StatSystem.Refactor.PC_Stats character)
    {
        _projectile = character._rangedWeapon._projectile;
        WeaponRange = character._rangedWeapon.range;
        ReloadTime =character._rangedWeapon.ReloadTimeBase;
        minimumDamage = character._rangedWeapon.MinDamage;
        maximumDamage = character._rangedWeapon.MaxDamage;
        accuracy = character.Accuracy;
        unit = GetComponentInParent<IUnit>();
        target.Setup(unit);
    }
    private void Start()
    {
        unit= GetComponentInParent<IUnit>();
        ReloadTime *= Mathf.Lerp(1, 0.5f, unit.Stats.ShootSpeed / 20f); 
        targetTemplate= GetComponentInChildren<FieldofView>();
        accuracy = Mathf.Clamp(accuracy, 1, 20);
        target.Setup(unit);
        Battle.Instance.Deploy += () => waitForDeployment = false;
    }
    [SerializeField] bool waitForDeployment=true;
    void Update()
    {
        if (waitForDeployment) return;
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
        var _target = target.CurrentTarget;
        if(_target.ValidTarget) 
            ShootAt?.Invoke(unit, _target.target.transform.position, _target.target, accuracy);
        //Shoot(projectile, Random.Range(minimumDamage, maximumDamage) * 50, target.Target.transform);
    }
    public float ShootPower
    {
        get => Random.Range(minimumDamage, maximumDamage) * 5;
    }
    private void OnDrawGizmos()
    {
        if (target== null || unit == null || unit.State != UnitState.Idle || target.ValidTargets.Count == 0) return;
        Gizmos.color = Color.green;
        
    }
}
