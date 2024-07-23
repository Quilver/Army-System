using JetBrains.Annotations;
using StatSystem;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;
namespace UnitType
{
    public class UnitR : UnitBase
    {
        #region Properties
        [SerializeField]
        StatSystem.UnitStats unitStats;
        public override StatSystem.UnitStats UnitStats => unitStats;

        [SerializeField]
        UnitState _state;
        public override UnitState State
        {
            get { return _state; }
            set
            {
                if (_state == UnitState.Fighting && value == UnitState.Idle)
                    foreach (var model in Models) model.STOPPED = false;
                _state = value;
            }
        }
        [SerializeField]
        UnitPositionR movement;
        Weapon weapon;
        public override UnitMovement.IMovement Movement
        {
            get
            {
                return movement;
            }
        }
        [SerializeField]
        GameObject _modelPrefab;
        #endregion
        #region Model information
        #endregion
        #region Initialise
        private void Start()
        {
            _state = UnitState.Idle;
            if (Battle.Instance.player.Units.Contains(this))
                unitStats.Load();
            var size = GetComponent<UnitSize>();
            movement.Init(this, size.UnitWidth);
            transform.position = Vector3.zero;
            InstantiateModels(size.StartingSize, size.UnitWidth);
            _maxUnitSize = size.StartingSize;
            Destroy(size);
            
            weapon = new Weapon(this);
        }
        #endregion
        #region Update
        private void Update()
        {
            if (State == UnitState.Fighting)
                weapon.UpdateCombat();
            movement.UpdateMovement();
            if (Models.Count == 0) Die();
        }
        #endregion
        #region Combat and death
        int _maxUnitSize;
        public void STOP()
        {
            foreach (var model in Models) model.STOPPED = true;
        }
        public override bool Wounded
        {
            get
            {
                return Models.Count <= _maxUnitSize / 2;
            }
        }

        
        public override void TakeDamage(int damage)
        {
            for (int i = 0; i < damage; i++)
            {
                if (Models.Count == 0)
                {
                    //Die();
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
        #endregion

        public override string ToString()
        {
            return UnitStats.ToString();
        }


    }
}

