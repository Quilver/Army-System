using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;
using UnityEngine.UIElements;
namespace MovementSystem
{
    public interface ISensors
    {
        public float SensorLength { get; }
        public List<RaycastHit2D> Sensors { get; }
        public Vector2 SensorDirection(RaycastHit2D hit);
        public RaycastHit2D ForwardSensor { get; }
        public RaycastHit2D RightWhisker { get; }
        public RaycastHit2D LeftWhisker { get; }

        public static LayerMask SensorLayerMask = 1<<3 | LayerMask.GetMask("Unit") | LayerMask.GetMask("Terrain");
        public static RaycastHit2D UnitCast(Vector2 position, Vector2 direction, Vector2 size)
        {
            return Physics2D.CircleCast(position, size.x, direction, SensorLayerMask);
        }
        public static RaycastHit2D RayCast(Vector2 position, float angle, float size)
        {
            Vector2 direction = Quaternion.AngleAxis(angle, Vector3.forward) * Vector3.up;
            return Physics2D.Raycast(position, direction, size, SensorLayerMask);
        }
        public static Collider2D[] CircleCast(Vector2 position, float radius)
        {
            return Physics2D.OverlapCircleAll(position, radius, SensorLayerMask);
        }
    }
}