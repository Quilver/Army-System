using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ModelComponents
{
    class WarriorToughness : ITakeDamage
    {
        IUnitData _unitData;
        IDeath _death;
        private void Start()
        {
            _unitData= GetComponent<IUnitData>();
            _death= GetComponent<IDeath>();
        }
        float DefenceRoll()
        {
            return Random.Range(0f, 3*(_unitData.UnitStats.Defence + 5));
        }
        //Rear angle should be 135, but I am cheating it to 120 so flanks are harder
        const float FLANKANGLE = 120;
        const int FLANKBONUS = 2;
        const float REARANGLE = 45;
        const int REARBONUS = 4;
        public override void TakeDamage(float damage, Transform attacker)
        {
            if(!enabled)return;
            var angle = Vector2.Angle(transform.up, (transform.position - attacker.position));
            //Rear attack bonus
            if (angle < REARANGLE)
                damage *= REARBONUS;
            //Flank attack bonus
            else if(angle < FLANKANGLE)
                damage *= FLANKBONUS;
            if (DefenceRoll() < damage)
            {
                _death.Die();
                LogKill(attacker.GetComponent<IUnit>(), _unitData.Unit);
            }
        }

        public override void TakeDamage(float damage)
        {
            if (!enabled) return;
            if (DefenceRoll() < damage)
            {
                _death.Die();
            }
        }
    }
}