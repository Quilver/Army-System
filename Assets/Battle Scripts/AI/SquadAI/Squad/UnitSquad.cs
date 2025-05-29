using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
namespace AISystem.Squads
{
    [RequireComponent(typeof(InfluenceMap))]
    public class UnitSquad : ISquad
    {
        [SerializeField]
        InfluenceMap influenceMap;
        List<IUnit> units;
        Dictionary<IUnit, UnitAI.UnitAIMono> unitAndTheirAI;
        public override List<IUnit> GetUnitsToOrder
        {
            get
            {
                if (units == null || units.Count != GetComponentsInChildren<UnitAI.UnitAIMono>().Length)
                {
                    units = new List<IUnit>();
                    unitAndTheirAI = new();
                    foreach (var unitAI in GetComponentsInChildren<UnitAI.UnitAIMono>())
                    {
                        if(unitAI.unit == null)continue;
                        unitAI.unit.UnitDestroyed += () => units.Remove(unitAI.unit);
                        units.Add(unitAI.unit);
                        unitAndTheirAI.Add(unitAI.unit, unitAI);
                    }
                }
                return units;
            }
        }
        private void Start()
        {
            StartCoroutine(InfluenceMapUpdate());
            
        }
        IEnumerator InfluenceMapUpdate()
        {
            influenceMap = GetComponent<InfluenceMap>();
            influenceMap.Setup(this);
            yield return null;
            while (true)
            {
                influenceMap.UpdateMapScores();
                yield return new WaitForSeconds(1);
            }
        }
        
        public enum DrawGizmoMode
        {
            None,
            UnitAI, 
            InfluenceMapAI,
            Both
        }
        [SerializeField] DrawGizmoMode DrawGizmosMode;
        private void OnDrawGizmos()
        {
            if (DrawGizmosMode == DrawGizmoMode.None) return;
            if (DrawGizmosMode == DrawGizmoMode.UnitAI) UnitAIGizmo();
            else if (DrawGizmosMode == DrawGizmoMode.UnitAI) InfluenceMapGizmo();
            else
            {
                InfluenceMapGizmo();
                UnitAIGizmo();
            }

        }
        protected override void OnDrawGizmosSelected()
        {
            
        }
        void UnitAIGizmo()
        {

        }
        void InfluenceMapGizmo()
        {
            influenceMap = GetComponent<InfluenceMap>();
            influenceMap.Setup(this);
            influenceMap.UpdateMapScores();
            influenceMap.Draw();
        }
    }
}