using Pathfinding;
using System;
using System.Collections;
using System.Collections.Generic;
//using System.Linq.Expressions;
using UnityEngine;


namespace UnitMovement
{
    public class RayMovement : MonoBehaviour, IMovement
    {
        #region Properties

        RegimentSizer _unitBody;
        ChargeSizer _charge;
        RegimentSizer IMovement.unitBody => _unitBody;
        ChargeSizer IMovement.charge => _charge;

        Vector2 _pos;
        int width;
        public Vector2 Location => _pos;
        [SerializeField, Range(-180f, 180f)]
        float angle;
        public float Rotation => angle;
        public int Files
        {
            get
            {
                if (unit.ModelsRemaining < width) width = unit.ModelsRemaining;
                return width;
            }
        }
        public int Ranks => Mathf.CeilToInt((unit.ModelsRemaining * 1.0f) / Files);
        [SerializeField, Range(8, 30)]
        float Range;
        UnitBase unit;
        Vector2 Destination
        {
            get
            {
                if (unit.State == UnitState.Idle)
                    destination = _pos;
                else if (targetEnemy != null)
                    destination = targetEnemy.position;
                return destination;
            }
        }
        [SerializeField, Range(0.05f, 1)]
        float stepSize = 0.1f;
        [SerializeField, Range(0.05f, 1)]
        float turnSpeed = 0.1f;
        [SerializeField]
        Vector2 destination;
        Vector2 NextMidpoint;
        Transform targetEnemy = null;
        #endregion

        #region Initialise 
        void Awake()
        {
            unit = GetComponent<UnitBase>();
        }
        public void Load(Vector2 pos, int width)
        {
            _pos = pos;
            this.width = width;
            destination = pos;
            transform.position = Vector3.zero;
            _charge = unit.GetComponentInChildren<ChargeSizer>();
            _unitBody = unit.GetComponentInChildren<RegimentSizer>();
        }
        #endregion
        #region Update and Inputs
        public void Update()
        {
            if (unit == null || unit.ModelsRemaining == 0) return;
            else if (unit.State == UnitState.Fighting) Pursuit();
            else if (unit.State == UnitState.Fleeing) Flee();
            else if (unit.State == UnitState.Moving && !unit.ModelsAreMoving)
                MoveToLocation();
        }
        public void MoveTo(Vector2 location)
        {
            if (unit.State == UnitState.Fighting) return;
            targetEnemy = null;
            destination = location;
            unit.State = UnitState.Moving;
        }
        public void MoveTo(UnitBase unit)
        {
            if (this.unit.State == UnitState.Fighting) return;
            targetEnemy = unit.GetComponentInChildren<RegimentSizer>().transform;
            this.unit.State = UnitState.Moving;
            destination = targetEnemy.position;
        }
        #endregion
        #region Pursuit and Flee
        void Pursuit()
        {
            _pos = unit.LeadModelPosition;
            targetEnemy = null;
            Vector2 dir = Quaternion.Euler(0, 0, angle) * Vector2.up;

            Debug.DrawLine(_pos, _pos + dir * 10, Color.white);
            if (_unitBody.Clipping) return;
            if (!_charge.UnitAhead) return;
            Vector2 advance = _pos + dir * 0.1f;
            _pos = advance;
        }
        void Flee()
        {

        }
        #endregion

        #region Basic helper functions
        Vector3 LPosition
        {
            get
            {
                Vector3 offset = Vector3.left * unit.ModelSize.x / 2;
                Vector3 rotatedPos = Quaternion.Euler(0, 0, angle) * offset;
                Vector3 pos = unit.LeftMostModelPosition + rotatedPos;
                return pos;
            }
        }
        Vector3 RPosition
        {
            get
            {
                Vector3 offset = Vector3.right * unit.ModelSize.x / 2;
                Vector3 rotatedPos = Quaternion.Euler(0, 0, angle) * offset;
                Vector3 pos = unit.RightMostModelPosition + rotatedPos;
                return pos;
            }
        }
        #endregion
        #region Move System

        Vector2 GetNextPoint()
        {
            if (CanMoveTo(Destination))
                return Destination;
            Vector2 nextPoint = unit.LeadModelPosition;
            var path = Battle.Instance.highLevelMap.A_StarSearch(unit.LeadModelPosition, Destination);
            if (path == null) return nextPoint;
            for (int i = 0; i < path.Count; i++)
            {
                if (!CanMoveTo(path[i])) continue;
                nextPoint = path[i];
                break;
            }

            return nextPoint;
        }

        void MoveToLocation()
        {
            if (Destination == _pos)
            {
                unit.State = UnitState.Idle;
                return;
            }
            NextMidpoint = GetNextPoint();
            var dir = NextMidpoint - (Vector2)unit.LeadModelPosition;
            float angleDest = 90 - Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            angle = LerpAngle(Rotation, angleDest, turnSpeed);
            if (Vector2.Distance(Location, NextMidpoint) < stepSize)
                _pos = NextMidpoint;
            else
                _pos += dir.normalized * stepSize;
        }
        float LerpAngle(float currentAngle, float goalAngle, float stepSize)
        {
            Quaternion current = Quaternion.Euler(0, currentAngle, 0);
            Quaternion goal = Quaternion.Euler(0, 360 - goalAngle, 0);
            return Quaternion.Lerp(current, goal, stepSize).eulerAngles.y;

        }
        bool CanMoveTo(Vector2 location)
        {

            var dir = location - (Vector2)unit.LeadModelPosition;
            float angle = 90 - Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            float Distance = Vector3.Distance(Location, location);
            var ray = RayPathfinder.UnitRay(angle, Range, unit, LPosition, RPosition, targetEnemy);
            if (Vector3.Distance(ray, Location) >= Distance)
                return true;
            return false;
        }


        #endregion
        void OnGizmosSelected()
        {
            if (unit == null) return;

            if(unit.State != UnitState.Moving) return;
            if (CanMoveTo(Destination)) Gizmos.DrawLine(_pos, Destination);
            else
            {
                var path = Battle.Instance.highLevelMap.A_StarSearch(unit.LeadModelPosition, Destination);
                Gizmos.DrawLine(_pos, path[1]);
            }
        }
    }
}