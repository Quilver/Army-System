using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ModelComponents
{
    class WarriorToughness : ITakeDamage
    {
        IUnitData _unitData;
        IDeath _death;
        Rigidbody2D _body;
        private void Start()
        {
            _unitData= GetComponent<IUnitData>();
            _death= GetComponent<IDeath>();
            _body= GetComponent<Rigidbody2D>();
        }
        float DefenceRoll()
        {
            return Random.Range(0f, 3*(_unitData.UnitStats.Defence + 5));
        }
        bool Kill(float defence, float damage)
        {
            //Debug.Log($"Damage: {damage} vs Defence {defence} => Hit chance: {Mathf.Lerp(85, 99, (10 + defence - damage) / 20)}");
            return Mathf.Lerp(85, 99, (10 + defence - damage) / 20) < Random.Range(0, 100);
        }
        //Rear angle should be 135, but I am cheating it to 120 so flanks are harder
        const float FLANKANGLE = 120;
        const int FLANKBONUS = 2;
        const float REARANGLE = 45;
        const int REARBONUS = 4;
        //Melee damage
        public override void TakeDamage(float damage, Transform attacker)
        {
            if(!enabled)return;
            Vector2 direction = (transform.position - attacker.position);
            var angle = Vector2.Angle(transform.up, direction);
            //Rear attack bonus
            if (angle < REARANGLE)
                damage *= REARBONUS;
            //Flank attack bonus
            else if(angle < FLANKANGLE)
                damage *= FLANKBONUS;
            _body.AddForce(DamageKnockBackMultiplier * damage * direction.normalized);
            if (Kill(_unitData.UnitStats.Defence, damage))
            {
                _death.Die();
                LogKill(attacker.GetComponentInParent<IUnitData>().Unit, _unitData.Unit);
            }
        }
        [SerializeField, Range(10, 100)] float DamageKnockBackMultiplier = 20;

        //Shooting damage
        public override void TakeDamage(float damage, Vector2 direction)
        {
            if (!enabled) return;
            _body.AddForce(DamageKnockBackMultiplier * damage * direction.normalized);
            GetComponent<SpriteRenderer>().material.SetVector("_HitForce", direction.normalized * damage * 0.2f);
            if (Kill(_unitData.UnitStats.Defence, damage))
            {
                _death.Die();
            }
        }
    }
}