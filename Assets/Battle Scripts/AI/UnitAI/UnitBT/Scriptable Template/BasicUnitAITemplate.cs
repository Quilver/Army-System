using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AISystem.Behaviour;
namespace AISystem.UnitAI
{
    [CreateAssetMenu(menuName = "AI/Basic unit AI")]
    public class BasicUnitAITemplate : UnitAITemplate
    {
        public BasicUnitAI basicAI;

        public override UnitAI unitAI => basicAI;


    }
}