using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AISystem.Behaviour;
using AISystem.Query;
namespace AISystem.UnitAI
{
    public class UnitAIMono : MonoBehaviour
    {
        [SerializeField] BasicUnitAITemplate unitAITemplate;
        //public bool startOnDeployment;
        public IUnit unit;
        [SerializeField] BasicUnitAI unitAI;
        
        // Start is called before the first frame update
        void Start()=> Setup();
        void Setup()
        {
            ISquad squad = GetComponentInParent<ISquad>();
            InfluenceMap influenceMap = GetComponentInParent<InfluenceMap>();
            if (unitAITemplate != null)
                unitAI = Instantiate(unitAITemplate).unitAI as BasicUnitAI;
            unitAI.Setup(unit, squad, influenceMap);
            StartCoroutine(unitAI.RunAI());
        }
    }
}