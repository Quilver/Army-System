using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AISystem.Behaviour;
namespace AISystem.UnitAI
{
    [CreateAssetMenu(menuName = "AI/Soldier AI")]
    public class ProfessionalUnitAITemplate : UnitAITemplate
    {
        public ProfessionalUnitAI professionalAI;

        public override UnitAI unitAI => professionalAI;


    }
}
