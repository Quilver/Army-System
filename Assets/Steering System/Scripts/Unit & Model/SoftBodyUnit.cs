using StatSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
namespace SoftBody
{
    public class SoftBodyUnit : UnitTemplate
    {
        #region Base Properties
        public List<Model> _models;
        int _width;
        float _modelSize;
        public override int Files => (ModelCount < _width) ? ModelCount : _width;
        public override int ModelCount => _models.Count;
        public override float ModelSize => _modelSize;
        [SerializeField]
        RegimentStats _unitStats;
        public override RegimentStats Stats => _unitStats;
        #endregion

        private void Start()
        {
            //
            UnitFormation modelFactory = GetComponent<UnitFormation>();
            _models = modelFactory.models;
            _width = modelFactory.Width;
            _modelSize = modelFactory.ModelSize;
            //
            toPosition= GetComponentInChildren<PathToPosition>();
            toTarget = GetComponentInChildren<PathToTarget>();
            circleRound = GetComponentInChildren<CircleRound>();
            seperate = GetComponentInChildren<Separate>();
            FinishedMoving += Breaks;
            Breaks(null);
            Transition += EnterCombat; Transition += EnterFlee;
        }

        #region Movement StateMachine
        void EnterIdle(UnitState current, UnitState next) { 
        
        }
        void ExitIdle(UnitState current, UnitState next) { 
        
        }
        void EnterMovement(UnitState current, UnitState next) { 
        
        }
        void ExitMovement(UnitState current, UnitState next) { 
        
        }
        void EnterCombat(UnitState current, UnitState next) { 

        }
        void ExitCombat(UnitState current, UnitState next) { 

        }
        [SerializeField, Range(3, 15)]
        float FleeTime = 6;
        void EnterFlee(UnitState current, UnitState next) {
            if (next != UnitState.Fleeing) return;
            Invoke("ExitFlee", FleeTime);
            MoveTowards(transform.position, null);
            Breaks(null);
            GetComponentInChildren<PathToPosition>().enabled = false;
            GetComponentInChildren<Flee>().enabled=true;
        }
        void ExitFlee() {
            unitState= UnitState.Idle;
            Breaks(GetComponentInChildren<Flee>());
        }
        #endregion



        PathToTarget toTarget;
        PathToPosition toPosition;
        CircleRound circleRound;
        Separate seperate;
        public Action<Vector2, Transform> MoveTowards;
        public Action<SteeringBehaviour> FinishedMoving;

        void Breaks(SteeringBehaviour behaviour)
        {
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (behaviour != null) {
                rb.drag = 10;
                rb.angularDrag = 10;
                rb.mass = 10;
                foreach (var model in _models) model.Move(false);
            }
            else
            {
                rb.mass = 1;
                rb.drag = 1;
                rb.angularDrag = 2;
                foreach (var model in _models) model.Move(true);
            }
        }
        public override void MoveTo(Vector2 position)
        {
            if(unitState == UnitState.Fleeing) return;

            MoveTowards(position, null);
            Breaks(null);
        }
        
        public override void MoveTo(Transform target)
        {
            if (unitState == UnitState.Fleeing) return;
            var unitTarget = target.GetComponent<SoftBodyUnit>();

            if (unitTarget != null && !army.EnemyUnits.Contains(unitTarget))
                return;
            Breaks(null);
            MoveTowards(target.position, target);
        }
        public override string ToString()
        {
            return _unitStats.UnitName + " " + _models.Count +"/"+_unitStats.Stats()[0].CurrentStat;
        }
        public override void TakeDamage(int modelsRemaining)
        {
            if (modelsRemaining <= _unitStats.Stats()[0].CurrentStat / 3)
            {
                Debug.Log(gameObject.name + ": is broken");
                GetComponent<UnitFormation>().DestroyUnit();
                Die();
            }
            else if(modelsRemaining <= _unitStats.Stats()[0].CurrentStat/2)
            {
                unitState = UnitState.Fleeing;
            }
            
        }
        HashSet<SoftBodyUnit> enemies;
        public void StartFight(SoftBodyUnit unitTarget)
        {
            if (unitTarget != null && !army.EnemyUnits.Contains(unitTarget))
                return;
            if(enemies == null) enemies = new HashSet<SoftBodyUnit>();
            enemies.Add(unitTarget);
            this.unitState = UnitState.Fighting;
        }

        
    }
}