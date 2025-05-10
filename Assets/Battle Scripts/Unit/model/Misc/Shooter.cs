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
            Vector2 direction = (position - (Vector2)transform.position);  
            float distance = direction.magnitude;
            direction = direction.normalized;
            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, distance, _shootMask);
            if (hit && hit.transform != target)
                return;
            Shot?.Invoke();
            var shot = Instantiate(_weapon.projectile);
            shot.transform.position = transform.position;
            shot.GetComponent<Projectile>().Setup(direction, _weapon.ShootPower, unit);
            RangedWeapons.ProjectileContainer.AddProjectile(shot.transform);
        }
    }
}
