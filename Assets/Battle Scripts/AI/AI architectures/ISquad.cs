using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
namespace AISystem
{
    public abstract class ISquad : MonoBehaviour
    {
        [SerializeField] bool guardPosition;
        public abstract List<IUnit> GetUnitsToOrder { get; }
        public virtual Vector2 Center
        {
            get
            {
                if(guardPosition) return transform.position;
                return MeanPos();
            }
        }
        public Vector2 MeanPos()
        {
            Vector3 center = Vector3.zero;
            foreach (var unit in GetUnitsToOrder)
            {
                if (unit == null) Debug.LogError($"{name} squad has not removed a destroyed unit");
                else
                    center += unit.transform.position;
            }
            return center / GetUnitsToOrder.Count;
        }
        public virtual Vector2 Direction
        {
            get
            {
                if (guardPosition) return transform.up;
                return MeanDir();
            }
        }
        protected Vector2 MeanDir()
        {
            Vector3 dir = Vector3.zero;
            foreach (var unit in GetUnitsToOrder)
                dir += unit.transform.up;
            return dir;
        }
        Army _squadArmy;
        public Army SquadArmy
        {
            get
            {
                if (_squadArmy == null) _squadArmy = GetUnitsToOrder[0].GetComponentInParent<Army>();
                return _squadArmy;
            }
        }

        //Gizmo and property Viewer
        [SerializeField] Vector2 _meanPos;
        [SerializeField] float _meanDirection;
        protected virtual void OnDrawGizmosSelected()
        {
            if (GetUnitsToOrder == null || GetUnitsToOrder.Count == 0) return;
            _meanPos = MeanPos(); _meanDirection = Vector2.SignedAngle(Vector2.up, MeanDir());
            Gizmos.color = Color.white;
            Gizmos.DrawRay(Center, 5f * Direction.normalized);
            
        }
    }
}
