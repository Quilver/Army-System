using RangedWeapons;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ModelComponents
{
    public class Shooter : MonoBehaviour
    {
        RangedWeapon _weapon;
        IUnitData _unitData;
        public event System.Action Shot;
        // Start is called before the first frame update
        void Start()
        {
            _unitData = GetComponentInParent<IUnitData>();
            _weapon = _unitData.Unit.GetComponentInChildren<RangedWeapon>();
            if(_weapon == null) return;
            _weapon.ShootAt += Shoot;
        }
        private void OnEnable()
        {
            if (_weapon == null) return;
            _weapon.ShootAt += Shoot;
        }
        private void OnDisable()
        {
            if (_weapon == null) return;
            _weapon.ShootAt -= Shoot;
        }
        private void OnDestroy()
        {
            if(_weapon == null) return;
            _weapon.ShootAt -= Shoot;
        }
        [SerializeField]
        LayerMask _shootMask;
        void Shoot(IUnit unit, Vector2 position, Transform target) {
            if (!_weapon._projectile.ValidShot(_unitData.Unit, transform, position, target)) return;
            Shot?.Invoke();
            var shot = Instantiate(_weapon._projectile);
            shot.transform.position = transform.position;
            shot.GetComponent<IProjectile>().Shoot(_unitData.Unit, position, target, _weapon.ShootPower);
            RangedWeapons.ProjectileContainer.AddProjectile(shot.transform);
        }
        private void OnDrawGizmosSelected()
        {
            if (_weapon == null) return;
            Transform target = _weapon.CurrentTarget;
            _weapon._projectile.GizmosValidShot(_unitData.Unit, transform, target.position, target);
            _weapon._projectile.GizmosFireRadius(_unitData.Unit, transform, target.position, target);
        }
    }
}
