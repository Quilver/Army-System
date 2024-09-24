using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;


namespace UnitMovement
{
    public class RayMovement : MonoBehaviour, IMovement
    {
        RegimentSizer _unitBody;
        ChargeSizer _charge;
        RegimentSizer IMovement.unitBody => _unitBody;
        ChargeSizer IMovement.charge => _charge;
        public void Load(Vector2 pos, int width)
        {
            _pos = pos;
            this.width = width;
            destination = pos;
            transform.position = Vector3.zero;
            _charge = unit.GetComponentInChildren<ChargeSizer>();
            _unitBody = unit.GetComponentInChildren<RegimentSizer>();
        }
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
                if(unit.ModelsRemaining < width) width= unit.ModelsRemaining;
                return width;
            }
        }
        public int Ranks => Mathf.CeilToInt((unit.ModelsRemaining* 1.0f) / Files);
        [SerializeField, Range(8, 30)]
        float Range;
        UnitBase unit;
        void Awake()
        {
            unit = GetComponent<UnitBase>();
        }
        public void Update()
        {
            if (unit == null || unit.ModelsRemaining == 0) return;
            else if (unit.State == UnitState.Fighting) Pursuit();
            else if (destination == Location) unit.State = UnitState.Idle;
            else if (!unit.ModelsAreMoving)
                UpdatePath();
        }
        void Pursuit()
        {
            _pos = unit.LeadModelPosition;
            if (_unitBody.Clipping) return;
            if (!_charge.UnitAhead) return;
            Vector2 advance = unit.LeadModelPosition + GetVectorFromAngle(angle) * 0.1f;
            if (_unitBody.CanBeOn(advance, Rotation, 2, Files, Ranks))
                _pos = advance;
        }
        #region RaySystem
        List<Vector3> UnitRayStarts(float angle)
        {
            List<Vector3> starts = new List<Vector3>();
            Vector3 L = Quaternion.Euler(0, 0, -angle) * new Vector3(unit.LOffset - unit.ModelSize.x/2,0) + unit.LeadModelPosition;
            Vector3 R = Quaternion.Euler(0, 0, -angle) * new Vector3(unit.ROffset + unit.ModelSize.x/2,0) + unit.LeadModelPosition;
            float steps = Vector3.Distance(L, R);
            if (steps < 2) steps = 2;
            for (float i = 0; i <= steps; i++)
                starts.Add(Vector3.Lerp(L, R, i / steps));
            starts.Add(R);    
            return starts;
        }
        Vector3 UnitRay(float angle)
        {
            float steps = Vector3.Distance(LPosition, RPosition);
            if (steps < 2) steps = 2;
            float maxDistance = Range;
            foreach (var start in UnitRayStarts(angle))
            {
                Vector3 end = ChargeRay(start, angle, maxDistance, targetEnemy);
                if (Vector3.Distance(start, end) < maxDistance)
                    maxDistance = Vector3.Distance(start, end) - 0.3f;
            }
            Vector3 ORay = ChargeRay(unit.LeadModelPosition, angle, maxDistance, targetEnemy);
            return ORay;
        }
        Vector3 Ray(Vector3 origin, float angle, float range, int recursionLimit = 0)
        {
            var raycast2D = Physics2D.Raycast(origin, GetVectorFromAngle(angle), range);
            if (raycast2D.collider == null)
                return origin + GetVectorFromAngle(angle) * range;
            else
            {
                if (raycast2D.collider.transform.parent == transform && recursionLimit <4)
                    return Ray((Vector3)raycast2D.point + GetVectorFromAngle(angle) * 0.1f, angle, range - Vector3.Distance(origin, raycast2D.point), recursionLimit+1);
                return raycast2D.point;
            }
        }
        Vector3 ChargeRay(Vector3 origin, float angle, float range, UnitBase target, int recursionLimit = 0)
        {
            if(target == null) return Ray(origin, angle, range);
            var rayCast2D = Physics2D.RaycastAll(origin, GetVectorFromAngle(angle), range);
            foreach (var hit in rayCast2D)
            {
                var unit = hit.collider.transform.parent;
                if (unit != transform && unit != target.transform)
                    return hit.point;
            }
            return origin + GetVectorFromAngle(angle) * range;
        }
        #endregion
        #region Basic helper functions
        static Vector3 GetVectorFromAngle(float angle)
        {
            float angleRad = angle * (Mathf.PI / 180f);
            return new Vector3(Mathf.Sin(angleRad), Mathf.Cos(angleRad));
        }
        static Vector3 RotateAroundPivot(Vector3 point, Vector3 pivot, float angle)
        {
            return Quaternion.Euler(0, 0, angle) * (point - pivot) + pivot;

        }

        
        
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
                Vector3 pos = unit.RightMostModelPosition +  rotatedPos;
                return pos ;
            }
        }
        #endregion
        #region Move System
        [SerializeField]
        Vector2 destination;
        Vector2 NextMidpoint;
        Vector2 GetNextPoint()
        {
            if (CanMoveTo(destination))
                return destination;
            Vector2 nextPoint = unit.LeadModelPosition;
            var path = Battle.Instance.highLevelMap.A_StarSearch(unit.LeadModelPosition, destination);
            for (int i = 0; i < path.Count; i++)
            {
                if (!CanMoveTo(path[i])) continue;
                nextPoint = path[i];
                break;
            }

            return nextPoint;
        }
        [SerializeField]
        float stepSize = 0.1f;
        void UpdatePath()
        {
            NextMidpoint = GetNextPoint();
            var dir = NextMidpoint - (Vector2)unit.LeadModelPosition;
            float angleDest = 90 - Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            angle = -LerpAngle(Rotation, angleDest, 1.5f);
            if (Vector2.Distance(Location, NextMidpoint) < stepSize)
                _pos = NextMidpoint;
            else
                _pos += dir.normalized * stepSize;
        }
        float LerpAngle(float currentAngle, float goalAngle, float stepSize)
        {
            currentAngle %= 360; goalAngle %= 360;
            if(currentAngle < 0) currentAngle = 360 + currentAngle;
            if (goalAngle< 0) goalAngle = 360 + goalAngle;
            float delta = Mathf.Abs(currentAngle - goalAngle);
            if (delta < 180) return Mathf.Lerp(currentAngle, goalAngle, stepSize);
            else return Mathf.Lerp(currentAngle, goalAngle, stepSize);

        }
        bool CanMoveTo(Vector2 location)
        {

            var dir = location - (Vector2)unit.LeadModelPosition;
            float angle = 90 - Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            float Distance = Vector3.Distance(Location, location);
            var ray = UnitRay(angle);
            if (Vector3.Distance(ray, Location) >= Distance)
                return true;
            return false;
        }
        private void OnDrawGizmos()
        {
            if(unit== null)
            {
                Vector3 l = RotateAroundPivot(transform.position + Vector3.left * 2, transform.position, this.angle);
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(l, 0.25f);
                Vector3 r = RotateAroundPivot(transform.position + Vector3.right * 2, transform.position, this.angle);
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(r, 0.25f);
            }
            else
            {
                Vector3 l = RotateAroundPivot(LPosition, unit.LeadModelPosition, this.angle);
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(LPosition, 0.25f);
                Vector3 r = RotateAroundPivot(RPosition, unit.LeadModelPosition, this.angle);
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(RPosition, 0.25f);
            }
            Gizmos.color = Color.black;
            if (unit == null) return;
            Gizmos.DrawSphere(unit.LeadModelPosition, 0.25f);
            Vector2 nextPoint = GetNextPoint();
            Gizmos.DrawSphere(nextPoint, 0.25f);
            Vector3 dir = (Vector3)nextPoint - unit.LeadModelPosition;
            Gizmos.color = Color.red;
            Gizmos.DrawRay(unit.LeadModelPosition, dir);
            Gizmos.color= Color.black;
            float angle = 90 - Mathf.Atan2(NextMidpoint.y - unit.LeadModelPosition.y, NextMidpoint.x - unit.LeadModelPosition.x) * Mathf.Rad2Deg;
            foreach (var start in UnitRayStarts(angle))
            {
                Gizmos.DrawRay(start, dir);
            }
        }
        public void MoveTo(Vector2 location)
        {
            if (unit.State == UnitState.Fighting)return;
            targetEnemy = null;
            destination = location;
            unit.State = UnitState.Moving;
        }
        UnitBase targetEnemy = null;
        public void MoveTo(UnitBase unit)
        {
            if (unit.State == UnitState.Fighting)return;
            targetEnemy = unit;
            destination = unit.LeadModelPosition;
            unit.State = UnitState.Moving;
        }
        #endregion
    }
}