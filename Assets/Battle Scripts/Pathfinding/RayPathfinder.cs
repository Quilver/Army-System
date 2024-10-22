using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding
{
    //This is a container for maths and formulas for RayMovement
    public class RayPathfinder
    {
        #region Ray Methods
        static List<Vector3> UnitRayStarts(float angle, UnitBase unit)
        {
            List<Vector3> starts = new();
            Vector3 L = Quaternion.Euler(0, 0, -angle) * new Vector3(unit.LOffset - unit.ModelSize.x / 2, 0) + unit.LeadModelPosition;
            Vector3 R = Quaternion.Euler(0, 0, -angle) * new Vector3(unit.ROffset + unit.ModelSize.x / 2, 0) + unit.LeadModelPosition;
            float steps = Vector3.Distance(L, R);
            if (steps < 2) steps = 2;
            for (float i = 0; i <= steps; i++)
                starts.Add(Vector3.Lerp(L, R, i / steps));
            starts.Add(R);
            return starts;
        }
        public static Vector3 UnitRay(float angle, float Range, UnitBase unit, Vector3 LPosition, Vector3 RPosition, Transform targetEnemy)
        {
            float steps = Vector3.Distance(LPosition, RPosition);
            if (steps < 2) steps = 2;
            float maxDistance = Range;
            foreach (var start in UnitRayStarts(angle, unit))
            {
                Vector3 end = ChargeRay(start, angle, maxDistance, unit.transform, targetEnemy);
                if (Vector3.Distance(start, end) < maxDistance)
                    maxDistance = Vector3.Distance(start, end) - 0.3f;
            }
            Vector3 ORay = ChargeRay(unit.LeadModelPosition, angle, maxDistance, unit.transform, targetEnemy);
            return ORay;
        }
        static Vector3 ChargeRay(Vector3 origin, float angle, float range, Transform CenterUnit, Transform targetEnemy)
        {
            var rayCast2D = Physics2D.RaycastAll(origin, GetVectorFromAngle(angle), range);
            foreach (var hit in rayCast2D)
            {
                var unit = hit.collider.transform.parent;
                if (unit == CenterUnit) continue;
                else if (targetEnemy != null && unit == targetEnemy.parent) continue;
                else return hit.point;
            }
            return origin + GetVectorFromAngle(angle) * range;
        }
        #endregion
        #region Static methods
        Vector3 UnitRay(float angle, Vector3 LPosition, Vector3 RPosition, 
            Vector3 LeadModelPosition, UnityEngine.Transform transform, float Range = float.MaxValue)
        {
            Vector3 L = RotateAroundPivot(LPosition, LeadModelPosition, -angle);
            Vector3 R = RotateAroundPivot(RPosition, LeadModelPosition, -angle);
            float steps = Vector3.Distance(L, R);
            if (steps < 2) steps = 2;
            float maxDistance = Range;
            for (float i = 0; i <= steps; i++)
            {
                Vector3 point = Vector3.Lerp(L, R, i / steps);
                Vector3 end = Ray(point, angle, maxDistance, transform);
                if (Vector3.Distance(point, end) < maxDistance)
                    maxDistance = Vector3.Distance(point, end) - 0.3f;
            }
            Vector3 ORay = Ray(LeadModelPosition, angle, maxDistance, transform);
            return ORay;
        }
        Vector3 Ray(Vector3 origin, float angle, float range, UnityEngine.Transform transform)
        {
            var raycast2D = Physics2D.Raycast(origin, GetVectorFromAngle(angle), range);
            if (raycast2D.collider == null)
                return origin + GetVectorFromAngle(angle) * range;
            else
            {
                if (raycast2D.collider.transform.parent == transform)
                    return Ray((Vector3)raycast2D.point + GetVectorFromAngle(angle) * 0.1f, angle, range - Vector3.Distance(origin, raycast2D.point), transform);
                return raycast2D.point;
            }
        }
        #endregion
        #region Basic helper functions

       public  static Vector3 GetVectorFromAngle(float angle)
        {
            float angleRad = angle * (Mathf.PI / 180f);
            return new Vector3(Mathf.Sin(angleRad), Mathf.Cos(angleRad));
        }
        public static Vector3 RotateAroundPivot(Vector3 point, Vector3 pivot, float angle)
        {
            return Quaternion.Euler(0, 0, angle) * (point - pivot) + pivot;

        }

        #endregion
    }
}
