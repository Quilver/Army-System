using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AISystem.Squads
{
    [RequireComponent(typeof(ISquad), typeof(InfluenceMap))]
    public class MoveSquadTo : MonoBehaviour
    {
        public ISquad squad;
        InfluenceMap map;   
        [SerializeField]    
        Vector2 GoToPosition;
        [SerializeField, Range(1, 10)]
        float speed = 3;
        void Start() {
            squad = GetComponent<ISquad>();
            map = GetComponent<InfluenceMap>();
        } 
        private void FixedUpdate()
        {
            if (Vector2.Distance(squad.MeanPos(), transform.position) < 10)
            {
                transform.position = Vector3.MoveTowards(transform.position, GoToPosition, speed * Time.fixedDeltaTime);
                map.directionOfSquadTravel = GoToPosition - (Vector2)transform.position;
            }
            else
                map.directionOfSquadTravel = Vector2.zero;
                
        }
        private void OnDrawGizmosSelected()
        {
            if (!enabled) return;
            Gizmos.DrawLine(transform.position, GoToPosition);
        }
    }
}