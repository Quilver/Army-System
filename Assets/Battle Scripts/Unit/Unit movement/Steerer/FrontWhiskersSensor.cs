using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SteeringSystem
{
    class FrontWhiskersSensor : MonoBehaviour, Sensors
    {
        [SerializeField, Range(1, 10)]
        float _sensorLength;
        public float SensorLength => _sensorLength;
        int _sensorCount = 3;
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
                    Update();
                    var sensor = _sensors;
                    _sensors = null;
                    return sensor;
                }

                return _sensors;
            }
        }
        Formation.IShape _formationData;
        void Start()
        {
            _formationData = transform.parent.GetComponentInChildren<Formation.IShape>();
        }
        void Update()
        {
            _sensors = new();
            _sensors.Add(BoxSensor(0 + transform.rotation.z * Mathf.Deg2Rad));
            _sensors.Add(BoxSensor(45 + transform.rotation.z * Mathf.Deg2Rad));
            _sensors.Add(BoxSensor(315 + transform.rotation.z * Mathf.Deg2Rad));
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
        RaycastHit2D BoxSensor(float angle)
        {

            Vector3 direction = Quaternion.AngleAxis(angle, transform.forward) * transform.up;
            float length = SensorLength;
            return Physics2D.BoxCast(Center, _formationData.SizeOfFormation * 0.5f, angle, direction, length - transform.localScale.y / 2, SensorLayerMask);


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
        private void OnDrawGizmos()
        {
            if (!_drawGizmo) return;
            if (_formationData == null) _formationData = transform.parent.GetComponentInChildren<Formation.IShape>();
            DrawBoxSensor(0 + transform.rotation.z * Mathf.Deg2Rad);
            DrawBoxSensor(45 + transform.rotation.z * Mathf.Deg2Rad);
            DrawBoxSensor(315 + transform.rotation.z * Mathf.Deg2Rad);
        }
        #endregion
    }
}
