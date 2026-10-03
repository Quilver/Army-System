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
            StartCoroutine(MakeAttacks());
        }
        float AttackPerXSeconds
        {
            get
            {
                float X = 10;
                return X / _unitData.UnitStats.AttackSpeed;
            }
        }
        IEnumerator MakeAttacks()
        {
            yield return new WaitForSeconds(Random.Range(0, AttackPerXSeconds));
            while (enabled)
            {
                if (_targeter.Targets.Count > 0 && _unitData.Unit.State != UnitState.Fleeing)
                    Attack();
                yield return new WaitForSeconds(AttackPerXSeconds);
            }
        }
        void Attack()
        {
            var target = _targeter.Target;
            MakeStrike();
            target.TakeDamage(_unitData.UnitStats.AttackPower, transform);
        }
    }
}