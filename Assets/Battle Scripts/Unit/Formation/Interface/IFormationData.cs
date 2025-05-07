using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Formation
{
    public interface IFormationData 
    {
        public float ModelSize { get; }
        public int ModelCount { get; }
        public int Width { get; }
        public List<GameObject> Models { get; }
    }
}