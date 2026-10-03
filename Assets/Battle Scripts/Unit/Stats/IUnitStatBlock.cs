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
        public List<RangedWeapons.IProjectile> RangedWeapons;
        #region Fields, Battle Stats
        public abstract int Movement { get; }
        public abstract int MoveForce { get; }
        public abstract int ModelCount { get; }
        public abstract int Defence { get; }
        public abstract int Leadership { get; }
        public abstract int AttackPower { get; }
        public abstract int AttackSpeed { get; }
        public abstract int ShootSpeed { get; }
        public abstract int Accuracy { get; }
        #endregion
        protected void SetDescription(string unitName, Sprite portrait, List<GameObject> modelPrefabs)
        {
            _unitName = unitName;
            _portrait = portrait;
            _modelPrefabs = modelPrefabs;
        }
        public override string ToString()
        {
            string statString = $"{_unitName}\n";
            statString += $"\tMovement Speed: {Movement}\tModel Count: {ModelCount}\n";
            statString += $"\tDefence: {Defence}\tLeadership: {Leadership}\n";
            statString += $"\tAttack: {AttackPower}\tAttack Speed: {AttackSpeed}\n";
            statString += $"\tAccuracy: {Accuracy}\tFire Rate: {ShootSpeed}";
            return statString;
        }
        public virtual string StatString()
        {
            string statString = "";
            statString += $"Movement Speed: {Movement}, Model Count: {ModelCount}, ";
            statString += $"Defence: {Defence}, Leadership: {Leadership}, ";
            statString += $"Attack: {AttackPower}, Attack Speed: {AttackSpeed}, ";
            statString += $"Accuracy: {Accuracy}, Fire Rate: {ShootSpeed}";
            return statString;
        }
    }
}
