using StatSystem;
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
        }





        PathToTarget toTarget;
        PathToPosition toPosition;
        public override void MoveTo(Vector2 position)
        {
            toPosition.target = position;
            toPosition.enabled = true;
            toTarget.enabled = false;
        }

        public override void MoveTo(Transform target)
        {
            toTarget.target = target;
            toPosition.enabled = false;
            toTarget.enabled = true;
        }

        public override void TakeDamage(int damage)
        {
            throw new System.NotImplementedException();
        }
    }
}