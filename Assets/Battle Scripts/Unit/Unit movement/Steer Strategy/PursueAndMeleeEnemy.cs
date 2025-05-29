using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MovementSystem.Reaction
{
    public class PursueAndMeleeEnemy : ISteerStrategy
    {
        [SerializeField] ISteeringBehaviour pursuit, melee;
        [SerializeField] Transform enemy;
        #region Getters
        IMoveOrders _moverOrders;
        IMoveOrders MoveOrders
        {
            get
            {
                if (_moverOrders == null) _moverOrders = GetComponentInParent<IMoveOrders>();
                return _moverOrders;
            }
        }
        IUnit _unit;
        IUnit Unit
        {
            get
            {
                if (_unit == null) _unit = GetComponentInParent<IUnit>();
                return _unit;
            }
        }
        IMovementData _movementData;
        IMovementData MovementData
        {
            get
            {
                if (_movementData == null) _movementData = GetComponentInParent<IMovementData>();
                return _movementData;
            }
        }
        #endregion
        protected override void EnableEvents()
        {
            //Unit.EnteredMelee += Enter;
            //Unit.ExitedMelee += Exit;
            MoveOrders.moving += (Vector2 pos) => Exit();
            MoveOrders.pursuing += Moving;
        }
        protected override void DisableEvents()
        {
            if (Unit == null) return;
            //Unit.EnteredMelee -= Enter;
            //Unit.ExitedMelee -= Exit;
            MoveOrders.pursuing -= Moving;
        }
        void Melee(bool combat)
        {
            //if (combat) Enter();
            //else Exit();
        }
        void Moving(Transform pos)
        {
            enemy = pos;
            Enter();
        }
        protected override void Exit()
        {
            base.Exit();
            //MoveOrders.MoveTo(transform.position);
            enemy = null;   
        }
        private void Update()
        {
            //if enemy is null exit
            if (enemy == null) Exit();
            //if next to enemy use melee steering, else use goto to path to enemy
            else MeleeOrPursuit(CloseTooEnemy());
        }
        [SerializeField, Range(5, 15)] float pursuitDistance = 10;
        bool CloseTooEnemy()
        {
            var ray = Physics2D.Raycast(Unit.transform.position, enemy.position - Unit.transform.position, pursuitDistance, 1<<6 | 1<<3 | 1<<8);
            return ray && ray.transform == enemy && ray.distance < pursuitDistance;
        }
        void MeleeOrPursuit(bool melee)
        {
            if(this.melee.enabled != melee) this.melee.enabled = melee;
            if(pursuit.enabled == melee) pursuit.enabled = !melee;
        }
        [SerializeField]
        bool DrawGizmo;
        private void OnDrawGizmos()
        {
            if (!DrawGizmo || enemy == null) return;
            if(CloseTooEnemy()) Gizmos.color = Color.red;
            else Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, pursuitDistance);
        }

    }
}