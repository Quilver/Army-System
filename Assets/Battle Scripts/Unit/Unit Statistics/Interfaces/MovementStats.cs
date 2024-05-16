using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace StatSystem
{
    public interface IMovementStats
    {
        public int MoveSpeed { get; }
        float SpeedFactor => 1 / 4f;
        public float Speed => MoveSpeed * SpeedFactor;
    }
}