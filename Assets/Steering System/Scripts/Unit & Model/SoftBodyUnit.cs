using StatSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
        UnitStats _unitStats;
        public override UnitStats Stats => _unitStats;
        #endregion

        private void Start()
        {
            //
            ModelFactory modelFactory = GetComponent<ModelFactory>();
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
        }





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

            MoveTowards(position, null);
            Breaks(null);
        }

        public override void MoveTo(Transform target)
        {
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
        public override void TakeDamage(int damage)
        {
            damage = Math.Clamp(damage, 0, ModelCount);
            for (int i = damage - 1; i >= 0; i--)
            {
                var model = _models[_models.Count - 1];
                _models.RemoveAt(_models.Count-1);
                Destroy(model.gameObject);
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