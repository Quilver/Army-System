using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace StatSystem
{
    public interface IUnitStatBlock
    {
        //Other
        public string UnitName { get; }
        public Sprite Portrait { get; }
        public GameObject ModelPrefab { get; }  
        //Stats
        public int Movement { get; }
        public int UnitSize { get; }
        public int Defence { get; }
        public int Leadership { get; }
        public int AttackPower { get; }
        public int AttackSpeed { get; }
        public int ShootSpeed { get; }
        public int Accuracy { get; }
    }
}
