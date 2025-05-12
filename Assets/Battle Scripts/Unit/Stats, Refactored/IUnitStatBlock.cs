using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace StatSystem.Refactor
{
    public abstract class IUnitStatBlock : ScriptableObject
    {
        //Unit Description
        [SerializeField]
        string _unitName;
        public virtual string UnitName { get=> _unitName; }
        [SerializeField]
        Sprite _portrait;
        public Sprite Portrait { get => _portrait; }
        [SerializeField]
        List<GameObject> _modelPrefabs;
        public List<GameObject> ModelPrefab { get => _modelPrefabs; }
        #region Fields, Battle Stats
        public abstract int Movement { get; }
        public abstract int ModelCount { get; }
        public abstract int Defence { get; }
        public abstract int Leadership { get; }
        public abstract int AttackPower { get; }
        public abstract int AttackSpeed { get; }
        public abstract int ShootSpeed { get; }
        public abstract int Accuracy { get; }
        #endregion
    }
}
