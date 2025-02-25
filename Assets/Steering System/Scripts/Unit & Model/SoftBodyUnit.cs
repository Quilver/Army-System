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
        List<Model> _models;
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
            }
            else
            {
                rb.mass = 1;
                rb.drag = 1;
                rb.angularDrag = 2;
            }
        }
        public override void MoveTo(Vector2 position)
        {

            MoveTowards(position, null);
            Breaks(null);
        }

        public override void MoveTo(Transform target)
        {
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
        private void OnCollisionEnter2D(Collision2D collision)
        {
            var otherUnit = collision.gameObject.GetComponent<UnitTemplate>();
            if (otherUnit != null)
                Notifications.StartFight(this, otherUnit);
        }
    }
}