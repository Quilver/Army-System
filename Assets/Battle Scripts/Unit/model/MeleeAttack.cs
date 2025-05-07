using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ModelComponents
{
    class MeleeAttack : MonoBehaviour
    {
        IUnitData _unitData;
        IMeleeTargeter _targeter;
        private void Awake()
        {
            _unitData = GetComponentInParent<IUnitData>();
            _targeter = GetComponent<IMeleeTargeter>();
        }
        float timeSinceLastAttack;
        private void Update()
        {
            timeSinceLastAttack += Time.deltaTime;
            if (timeSinceLastAttack <= 10 / _unitData.UnitStats.AttackSpeed.CurrentStat)
            {
                timeSinceLastAttack = 0;
                if (_targeter.Targets.Count == 0) return;
                _targeter.Target.TakeDamage(_unitData.UnitStats.AttackPower.CurrentStat, transform);
            }
        }
    }
}