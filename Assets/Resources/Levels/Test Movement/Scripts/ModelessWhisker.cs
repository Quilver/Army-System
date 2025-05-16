using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MovementSystem
{
    class ModelessWhisker : MonoBehaviour, ISensors
    {
        [SerializeField, Range(1, 10)]
        float _sensorLength;
        public float SensorLength => _sensorLength;
        [SerializeField]
        LayerMask SensorLayerMask;
        List<RaycastHit2D> _sensors;
        public List<RaycastHit2D> Sensors
        {
            get
            {
                if (_sensors == null)
                {
                    UpdateRaycasts();
                    var sensor = _sensors;
                    _sensors = null;
                    return sensor;
                }

                return _sensors;
            }
        }
        public RaycastHit2D ForwardSensor => Sensors[0];
        public RaycastHit2D RightWhisker => Sensors[1];
        public RaycastHit2D LeftWhisker => Sensors[2];
        Rigidbody2D _body;
        void Awake()
        {
            _body = GetComponentInParent<Rigidbody2D>();
        }
        Dictionary<RaycastHit2D, Vector2> _raycastDirection;
        void FixedUpdate()
        {
            UpdateRaycasts();
        }
        void UpdateRaycasts()
        {
            _raycastDirection = new();
            _sensors = new();
            void AddSensorVelocity(float angle)
            {
                var ray = BoxSensorVelocity(angle);
                _sensors.Add(ray);
                if (_raycastDirection.ContainsKey(ray))
                    _raycastDirection[ray] = GetDirection(angle);
                else
                    _raycastDirection.Add(ray, GetDirection(angle));
            }
            if (_body != null || _body.velocity.sqrMagnitude > 0)
            {
                float forwardAngle = Vector2.Angle(Vector2.right, _body.velocity.normalized) * Mathf.Deg2Rad;
                AddSensorVelocity(forwardAngle);
                AddSensorVelocity(forwardAngle + 45);
                AddSensorVelocity(forwardAngle + 315);
            }
        }
        public Vector2 SensorDirection(RaycastHit2D hit)
        {
            if (_raycastDirection == null) UpdateRaycasts();
            return _raycastDirection[hit];
        }
        Vector3 Center
        {
            get
            {
                Vector3 offset = transform.up;
                offset.x *= 0;
                offset.y *= 0;
                return transform.position + offset;
            }
        }

        

        Vector2 GetDirection(float angle) => Quaternion.AngleAxis(angle, transform.forward) * transform.up;
        RaycastHit2D BoxSensor(float angle)
        {
            Vector3 direction = Quaternion.AngleAxis(angle, transform.forward) * transform.up;
            float length = SensorLength * _body.velocity.magnitude;
            return Physics2D.BoxCast(Center, Vector2.one * 0.5f, angle, direction, length - transform.localScale.y / 2, SensorLayerMask);


        }
        RaycastHit2D BoxSensorVelocity(float angle)
        {

            Vector3 direction = Quaternion.AngleAxis(angle, transform.forward) * _body.velocity.normalized;
            Vector3 size = Vector2.one * 0.5f;
            return Physics2D.BoxCast(Center, size, angle, direction, SensorLength * _body.velocity.magnitude, SensorLayerMask);


        }
        #region Gizmo Display
        [SerializeField]
        bool _drawGizmo;
        void DrawBoxSensorVelocity(float angle, float speed, Vector2 dir)
        {
            var angleAdjusted = Quaternion.AngleAxis(angle, transform.forward);
            var perpendicular = Vector2.Perpendicular(dir) * 0.5f;
            var right = Center - angleAdjusted * perpendicular;
            var left = Center + angleAdjusted * perpendicular;
            Vector3 direction = angleAdjusted * dir;
            float length = SensorLength * _body.velocity.magnitude;

            if (BoxSensorVelocity(angle))
            {
                length = BoxSensorVelocity(angle).distance;
                Gizmos.color = Color.red;
            }
            else
                Gizmos.color = Color.blue;
            Gizmos.DrawLine(left, right);
            Gizmos.DrawLine(left, left + direction * length);
            Gizmos.DrawLine(right, right + direction * length);
            Gizmos.DrawLine(left + direction * length, right + direction * length);
        }
        [SerializeField] Vector2 _GizmoForward = Vector2.up;
        private void OnDrawGizmos()
        {
            if (!_drawGizmo) return;
            if(_body==null)_body=GetComponentInParent<Rigidbody2D>();
            if (_body != null || _body.velocity.sqrMagnitude > 0.05f)
            {
                float forwardAngle = Vector2.Angle(Vector2.right, _body.velocity.normalized) * Mathf.Deg2Rad;
                DrawBoxSensorVelocity(0 + forwardAngle, _body.velocity.magnitude, _body.velocity.normalized);
                DrawBoxSensorVelocity(45 + forwardAngle, _body.velocity.magnitude, _body.velocity.normalized);
                DrawBoxSensorVelocity(315 + forwardAngle, _body.velocity.magnitude, _body.velocity.normalized);
            }
            
        }
        #endregion
    }
}
