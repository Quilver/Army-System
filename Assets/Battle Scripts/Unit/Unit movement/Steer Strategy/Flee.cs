using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
namespace MovementSystem.Reaction
{
    public class Flee : ISteerStrategy
    {
        IUnit _unit;
        IUnit Unit { 
            get { 
                if (_unit == null) _unit = GetComponentInParent<IUnit>();
                return _unit; 
            } 
        }
        Rigidbody2D _body;
        Rigidbody2D Body
        {
            get
            {
                if(_body == null) _body = GetComponentInParent<Rigidbody2D>();
                return _body;
            }
        }
        ArmyData _army;
        ArmyData _Army
        {
            get
            {
                if (_army == null)_army = GetComponentInParent<ArmyData>();
                return _army;
            }
        }
        [SerializeField]
        bool _fleeing;
        protected override void EnableEvents()
        {
            Unit.StateChanged += React;
        }

        protected override void DisableEvents()
        {
            Unit.StateChanged += React;
        }
        void React(UnitState state)
        {
            if (state != UnitState.Fleeing)
            {
                _fleeing = false;

            }
            else
            {
                Enter();
            }
        }
        protected override void Enter()
        {
            base.Enter();
            _fleeing = true;
            Body.AddForce(FleeDirection() * _forceMultiple);
        }
        protected override void Exit()
        {
            base.Exit();
            _fleeing = false;
        }
        private void Update()
        {
            FleeFromEnemy();
        }
        [SerializeField, Range(50, 300)]
        float _forceMultiple;
        void FleeFromEnemy()
        {
            Body.AddForce(FleeDirection() * Time.deltaTime * _forceMultiple);

        }
        Vector2 FleeDirection()
        {
            if(NearestEnemy() == null)return Vector2.zero;
            return (transform.position - NearestEnemy().position).normalized;
        }
        Transform NearestEnemy()
        {
            if(_Army == null) return null;
            float bestDistance = 1000;
            Transform nearestEnemy = null;
            var enemies = _Army.Enemies;
            foreach(var enemy in enemies) 
                if(Vector2.Distance(transform.position, enemy.transform.position) < bestDistance)
                {
                    nearestEnemy = enemy.transform;
                    bestDistance = Vector2.Distance(transform.position, enemy.transform.position);
                }
            return nearestEnemy;
        }

        [SerializeField]
        bool DrawGizmo;
        private void OnDrawGizmos()
        {
            if(!DrawGizmo) return;
            Gizmos.DrawRay(transform.position, FleeDirection());
        }
    }
}