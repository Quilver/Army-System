using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Formation
{
    class FormationData : MonoBehaviour, IFormationData
    {
        UnitTemplate _unit;
        void Start()
        {
            _unit = GetComponentInParent<UnitTemplate>();
        }
        [SerializeField, Range(0.5f, 2f)]
        float _modelSize;
        public float ModelSize => _modelSize;
        [SerializeField, Range(1, 8)]
        int _width;
        public int Width => (_width > ModelCount)? ModelCount : _width;
        [SerializeField, Range(1, 32)]
        int _modelCount;
        public int ModelCount => _modelCount;
    }
}