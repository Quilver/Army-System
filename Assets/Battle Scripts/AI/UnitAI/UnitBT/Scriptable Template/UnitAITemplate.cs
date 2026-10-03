using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AISystem.Behaviour;
namespace AISystem.UnitAI
{
    public abstract class UnitAITemplate : ScriptableObject
    {
        public abstract UnitAI unitAI { get; }
    }
}
