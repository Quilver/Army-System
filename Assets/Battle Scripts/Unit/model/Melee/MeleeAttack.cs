using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ModelComponents
{
    class MeleeAttack : IMeleeAttack
    {
        IUnitData _unitData;
        IMeleeTargeter _targeter;
        private void Start()
        {
            _unitData = GetComponentInParent<IUnitData>();
            _targeter = GetComponent<IMeleeTargeter>(); 
            timeSinceLastAttack = Random.Range(0, AttackPerXSeconds);
        }
        float AttackPerXSeconds
        {
            get
            {
                float X = 6;
                return X / _unitData.UnitStats.AttackSpeed;
            }
        }
        float timeSinceLastAttack;
        [SerializeField]
        private void Update()
        {
            timeSinceLastAttack += Time.deltaTime;
            if (timeSinceLastAttack >= AttackPerXSeconds)
            {
                timeSinceLastAttack = 0;
                if (_targeter.Targets.Count == 0 || _unitData.Unit.State == UnitState.Fleeing) return;
                Attack();
            }
        }
        float _attackForceMultiplier = 100;
        void Attack()
        {
            var target = _targeter.Target;
            MakeStrike();
            target.GetComponentInParent<Rigidbody2D>().AddForce(
                (target.transform.position - transform.position).normalized 
                * _unitData.UnitStats.AttackPower * _attackForceMultiplier);
            target.TakeDamage(_unitData.UnitStats.AttackPower, transform);
        }
    }
}