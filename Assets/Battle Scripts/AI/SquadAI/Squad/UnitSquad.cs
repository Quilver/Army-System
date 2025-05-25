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
        public override List<IUnit> GetUnitsToOrder
        {
            get
            {
                if (units == null)
                {
                    units = new List<IUnit>();
                    foreach (var unitAI in GetComponentsInChildren<UnitAI.UnitAIMono>())
                        units.Add(unitAI.unit);
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