using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
//using static UnityEngine.RuleTile.TilingRuleOutput;

namespace Pathfinding
{
    //This is a container for maths and formulas for RayMovement
    public class RayPathfinder
    {
        #region A*

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
        static Vector3 GetVectorFromAngle(float angle)
        {
            float angleRad = angle * (Mathf.PI / 180f);
            return new Vector3(Mathf.Sin(angleRad), Mathf.Cos(angleRad));
        }
        static Vector3 RotateAroundPivot(Vector3 point, Vector3 pivot, float angle)
        {
            return Quaternion.Euler(0, 0, angle) * (point - pivot) + pivot;

        }

        #endregion
    }
}
