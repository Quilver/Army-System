using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ModelComponents
{
    class MeleeAttack : IMeleeAttack
    {
        IUnitData _unitData;
        IMeleeTargeter _targeter;
        Rigidbody2D _body2D;
        Rigidbody2D Body
        {
            get
            {
                if(_body2D == null) _body2D = GetComponentInParent<Rigidbody2D>();
                return _body2D;
            }
        }
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
        [SerializeField, Range(10, 100)]
        float _attackForceMultiplier = 60;
        [SerializeField, Range(0, 1)]
        float _knockBackModifier = 0.2f;
        void Attack()
        {
            var target = _targeter.Target;
            MakeStrike();
            Vector2 dir = (target.transform.position - transform.position).normalized;
            float force = _unitData.UnitStats.AttackPower * _attackForceMultiplier;
            Body.AddForce(-force * _knockBackModifier * dir);
            target.GetComponentInParent<Rigidbody2D>().AddForce(dir*force);
            target.TakeDamage(_unitData.UnitStats.AttackPower, transform);
        }
    }
}