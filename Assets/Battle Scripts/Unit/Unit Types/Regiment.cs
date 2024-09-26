using StatSystem;
using System.Collections;
using System.Collections.Generic;
using UnitMovement;
using UnityEditor;
using UnityEngine;
namespace UnitType
{
    public class Regiment : UnitBase
    {
        [SerializeField]
        UnitStats stats;
        Weapon weapon;
        public override UnitStats UnitStats => stats;
        [SerializeField]
        UnitState _state;
        public override UnitState State
        {
            get { return _state; }
            set
            {
                //if (_state == UnitState.Fighting && value == UnitState.Idle)
                //    foreach (var model in models) model.STOPPED = false;
                _state = value;
            }
        }
        RayMovement movement;
        public override IMovement Movement { 
            get { 
                if(movement == null)
                    movement=GetComponent<RayMovement>();
                return movement; 
            } 
        }
        public override bool Wounded => false;
        public override void TakeDamage(int damage)
        {
            for (int i = 0; i < damage; i++)
            {
                if (Models.Count == 0)
                {
                    Die();
                    return;
                }
                Destroy(Models[Models.Count - 1].gameObject);
                Models.RemoveAt(Models.Count - 1);

            }
        }
        void Die()
        {
            if (this == null) return;
            Notifications.Died(this);
            //Battle.Instance.EndCombat(this);
            Destroy(this.gameObject);
        }
        public void Start() {
            _state = UnitState.Idle;
            if (Battle.Instance.player.Units.Contains(this))
                UnitStats.Load();
            var size = GetComponent<UnitSize>();
            (Movement as RayMovement).Load(transform.position, size.UnitWidth);
            InstantiateModels(size.StartingSize, size.UnitWidth);
            Destroy(size);
            weapon = new Weapon(this);
        }
        public void Update()
        {
            if (State == UnitState.Fighting)
                weapon.UpdateCombat();
        }
    }
}