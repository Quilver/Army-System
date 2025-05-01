using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ModelComponents
{
    class WarriorToughness : ITakeDamage
    {
        IUnitData _unitData;
        private void Start()
        {
            _unitData= GetComponent<IUnitData>();
        }
        public override void TakeDamage(float damage, Transform attacker)
        {
            float defenceScore = Random.Range(0f, _unitData.UnitStats.Defence.CurrentStat + 5);
            if (defenceScore < damage)
            {
                //_unitData.Formation.Death(this);
                //unit.GetComponent<UnitFormation>().Death(this);
                //Notifications.MeleeDamage(attacker, unit, 1);
            }
        }

        public override void TakeDamage(float damage)
        {
            throw new System.NotImplementedException();
        }
    }
}