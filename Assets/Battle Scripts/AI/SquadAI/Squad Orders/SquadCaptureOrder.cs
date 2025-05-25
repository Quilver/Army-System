using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AISystem.Squads
{
    [RequireComponent(typeof(ISquad), typeof(InfluenceMap))]
    public class SquadCaptureOrder : MonoBehaviour
    {
        public ISquad squad;
        InfluenceMap map;
        [SerializeField]
        CapturePoint capturePoint;
        [SerializeField, Range(1, 10)]
        float speed = 3;
        void Start()
        {
            squad = GetComponent<ISquad>();
            map = GetComponent<InfluenceMap>();
        }
        private void FixedUpdate()
        {
            if (Vector2.Distance(squad.MeanPos(), transform.position) < 10)
            {
                transform.position = Vector3.MoveTowards(transform.position, capturePoint.transform.position, speed * Time.fixedDeltaTime);
                map.directionOfSquadTravel = capturePoint.transform.position - transform.position;
            }
            else
                map.directionOfSquadTravel = Vector2.zero;

        }
        private void OnDrawGizmosSelected()
        {
            if (!enabled) return;
            Gizmos.DrawLine(transform.position, capturePoint.transform.position);
        }
    }
}
