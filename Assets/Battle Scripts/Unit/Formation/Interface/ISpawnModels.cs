using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Formation
{
    public interface ISpawnModels 
    {
        void SpawnUnit();
        public List<GameObject> Models { get; } 
    }
}