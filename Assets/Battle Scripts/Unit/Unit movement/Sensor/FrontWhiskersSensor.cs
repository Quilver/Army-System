using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MovementSystem
{
    class FrontWhiskersSensor : MonoBehaviour, ISensors
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
                    _formationData = transform.parent.GetComponentInChildren<Formation.IShape>();
                    UpdateRaycasts();
                    var sensor = _sensors;
                    _sensors = null;
                    return sensor;
                }

                return _sensors;
            }
        }
        public RaycastHit2D ForwardSensor => throw new System.NotImplementedException();
        public RaycastHit2D RightWhisker => throw new System.NotImplementedException();
        public RaycastHit2D LeftWhisker => throw new System.NotImplementedException();
        Formation.IShape _formationData;
        Rigidbody2D _body;
        void Start()
        {
            _formationData = transform.parent.GetComponentInChildren<Formation.IShape>();
            _body = GetComponentInParent<Rigidbody2D>();
        }
        [SerializeField]
        List<bool> SensorHitting;
        Dictionary<RaycastHit2D, Vector2> _raycastDirection;
        void Update()
        {
            UpdateRaycasts();
        }
        void UpdateRaycasts()
        {
            _raycastDirection = new();
            _sensors = new();
            void AddRay(float angle)
            {
                var ray = BoxSensor(angle);
                _sensors.Add(ray);
                if (_raycastDirection == null) Debug.LogError("dictionary is not initialised");
                if (_raycastDirection.ContainsKey(ray))
                    _raycastDirection[ray] = GetDirection(angle);
                else
                _raycastDirection.Add(ray, GetDirection(angle));
            }
            void AddRay2(float angle)
            {
                var ray = BoxSensorVelocity(angle);
                _sensors.Add(ray);
                if (_raycastDirection.ContainsKey(ray))
                    _raycastDirection[ray] = GetDirection(angle);
                else
                    _raycastDirection.Add(ray, GetDirection(angle));
            }
            if (_body == null || _body.linearVelocity.sqrMagnitude == 0)
            {
                AddRay(0 + transform.rotation.z * Mathf.Deg2Rad);
                AddRay(45 + transform.rotation.z * Mathf.Deg2Rad);
                AddRay(315 + transform.rotation.z * Mathf.Deg2Rad);
            }
            else
            {
                float forwardAngle = Vector2.Angle(Vector2.right, _body.linearVelocity.normalized) * Mathf.Deg2Rad;
                AddRay2(forwardAngle);
                AddRay2(forwardAngle+45);
                AddRay2(forwardAngle+315);
            }
        }
        public Vector2 SensorDirection(RaycastHit2D hit)
        {
            if(_raycastDirection == null) UpdateRaycasts();
            return _raycastDirection[hit];
        }
        Vector3 Center
        {
            get
            {
                Vector3 offset = transform.up;
                offset.x *= _formationData.OffsetFromUnit.y;
                offset.y *= _formationData.OffsetFromUnit.y;
                return transform.position + offset;
            }
        }

        

        Vector2 GetDirection(float angle)=> Quaternion.AngleAxis(angle, transform.forward)* transform.up;
        RaycastHit2D BoxSensor(float angle)
        {
            Vector3 direction = Quaternion.AngleAxis(angle, transform.forward) * transform.up;
            float length = SensorLength;
            return Physics2D.BoxCast(Center, _formationData.SizeOfFormation * 0.5f, angle, direction, length - transform.localScale.y / 2, SensorLayerMask);


        }
        RaycastHit2D BoxSensorVelocity(float angle)
        {

            Vector3 direction = Quaternion.AngleAxis(angle, transform.forward) * _body.linearVelocity.normalized;
            Vector3 size = _formationData.SizeOfFormation * 0.5f;
            return Physics2D.BoxCast(Center, size, angle, direction, SensorLength - transform.localScale.y / 2, SensorLayerMask);


        }
        #region Gizmo Display
        [SerializeField]
        bool _drawGizmo;
        void DrawBoxSensor(float angle)
        {
            var right = Center + Quaternion.AngleAxis(angle, transform.forward) * transform.right * transform.localScale.x * 0.5f * _formationData.SizeOfFormation.x;
            var left = Center - Quaternion.AngleAxis(angle, transform.forward) * transform.right * transform.localScale.x * 0.5f * _formationData.SizeOfFormation.x;
            Vector3 direction = Quaternion.AngleAxis(angle, transform.forward) * transform.up;
            float length = SensorLength;
            if (BoxSensor(angle))
            {
                length = BoxSensor(angle).distance;
                Gizmos.color = Color.red;
            }
            else
                Gizmos.color = Color.blue;
            Gizmos.DrawLine(left, right);
            Gizmos.DrawLine(left, left + direction * length);
            Gizmos.DrawLine(right, right + direction * length);
            Gizmos.DrawLine(left + direction * length, right + direction * length);
        }
        void DrawBoxSensorVelocity(float angle)
        {
            var angleAdjusted = Quaternion.AngleAxis(angle, transform.forward);
            var perpendicular = Vector2.Perpendicular(_body.linearVelocity.normalized) * 0.5f * _formationData.SizeOfFormation.x;
            var right = Center - angleAdjusted * perpendicular;
            var left = Center + angleAdjusted * perpendicular;
            Vector3 direction = angleAdjusted * _body.linearVelocity.normalized;
            float length = SensorLength;
            
            if (BoxSensor(angle))
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
        private void OnDrawGizmos()
        {
            if (!_drawGizmo) return;
            if (_formationData == null) _formationData = transform.parent.GetComponentInChildren<Formation.IShape>();
            if (_body == null || _body.linearVelocity.sqrMagnitude == 0)
            {
                DrawBoxSensor(0 + transform.rotation.z * Mathf.Deg2Rad);
                DrawBoxSensor(45 + transform.rotation.z * Mathf.Deg2Rad);
                DrawBoxSensor(315 + transform.rotation.z * Mathf.Deg2Rad);
            }
            else
            {
                float forwardAngle = Vector2.Angle(Vector2.right, _body.linearVelocity.normalized) * Mathf.Deg2Rad;
                DrawBoxSensorVelocity(0 + forwardAngle);
                DrawBoxSensorVelocity(45 + forwardAngle);
                DrawBoxSensorVelocity(315 + forwardAngle);
            }
        }
        #endregion
    }
}
