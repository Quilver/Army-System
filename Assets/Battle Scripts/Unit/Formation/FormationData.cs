using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Formation
{
    class FormationData : MonoBehaviour, IFormationData
    {
        IUnit _unit;
        ISpawnModels _spawnModels;
        void Start()
        {
            _spawnModels = GetComponent<ISpawnModels>();
            _unit = GetComponentInParent<IUnit>();
        }
        [SerializeField, Range(0.5f, 2f)]
        float _modelSize;
        public float ModelSize => _modelSize;
        [SerializeField, Range(1, 8)]
        int _width;
        public int Width => (_width > ModelCount)? ModelCount : _width;
        [SerializeField, Range(1, 32)]
        int _modelCount;
        public int ModelCount
        {
            get
            {
                if(Models !=null)return Models.Count;
                if(_unit == null)_unit = GetComponentInParent<IUnit>();
                return _unit.Stats.ModelCount.CurrentStat;
            }
        }
        public List<GameObject> Models
        {
            get
            {
                if (_spawnModels == null) return null;
                return _spawnModels.Models;
            }
        }
    }
}