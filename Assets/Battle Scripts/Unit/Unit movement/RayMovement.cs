using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace UnitMovement
{
    public class RayMovement : MonoBehaviour, IMovement
    {
        public void Load(Vector2 pos, int width)
        {
            _pos = pos;
            this.width = width;
            destination = pos;
            transform.position = Vector3.zero;
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
        #region RaySystem
        List<Vector3> UnitRayStarts(float angle)
        {
            List<Vector3> starts = new List<Vector3>();
            Vector3 L = Quaternion.Euler(0, 0, angle) * new Vector3(unit.LOffset - unit.ModelSize.x/2,0) + unit.LeadModelPosition;
            Vector3 R = Quaternion.Euler(0, 0, angle) * new Vector3(unit.ROffset + unit.ModelSize.x/2,0) + unit.LeadModelPosition;
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
                Vector3 end = Ray(start, angle, maxDistance);
                if (Vector3.Distance(start, end) < maxDistance)
                    maxDistance = Vector3.Distance(start, end) - 0.3f;
            }
            Vector3 ORay = Ray(unit.LeadModelPosition, angle, maxDistance);
            return ORay;
        }
        Vector3 Ray(Vector3 origin, float angle, float range)
        {
            var raycast2D = Physics2D.Raycast(origin, GetVectorFromAngle(angle), range);
            if (raycast2D.collider == null)
                return origin + GetVectorFromAngle(angle) * range;
            else
            {
                if (raycast2D.collider.transform.parent == transform)
                    return Ray((Vector3)raycast2D.point + GetVectorFromAngle(angle) * 0.1f, angle, range - Vector3.Distance(origin, raycast2D.point));
                return raycast2D.point;
            }
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
        void Update()
        {
            if(unit.ModelsAreMoving || destination == Location) return;
            Vector2 nextPoint = GetNextPoint();
            var dir = nextPoint - (Vector2)unit.LeadModelPosition;
            float angleDest = 90 - Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            angle = Mathf.Lerp(Rotation, -angleDest, 0.4f);
            float stepSize = 0.1f;
            if (Vector2.Distance(Location, destination) < stepSize)
                _pos = destination;
            else
                _pos += dir.normalized * stepSize;
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
            float angle = 90 - Mathf.Atan2(destination.y - unit.LeadModelPosition.y, destination.x - unit.LeadModelPosition.x) * Mathf.Rad2Deg;
            foreach (var start in UnitRayStarts(angle))
            {
                Gizmos.DrawRay(start, dir);
            }
        }
        public void MoveTo(Vector2 location)
        {
            destination = location;
        }
        public void MoveTo(UnitBase unit)
        {
            throw new System.NotImplementedException();
        }
        #endregion
    }
}