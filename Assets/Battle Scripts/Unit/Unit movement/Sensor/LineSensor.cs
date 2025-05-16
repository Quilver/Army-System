using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
namespace MovementSystem
{
    class LineSensor : MonoBehaviour, ISensors
    {
        [SerializeField, Range(1, 10)]
        float _sensorLength;
        public float SensorLength => _sensorLength;
        [SerializeField, Range(8, 24)]
        int _sensorCount;
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
                    _sensors=null;
                    return sensor;
                }

                return _sensors;
            }
        }
        public RaycastHit2D ForwardSensor => Sensors[0];
        public RaycastHit2D RightWhisker => Sensors[1];
        public RaycastHit2D LeftWhisker => Sensors[2];
        Formation.IShape _formationData;
        void Start()
        {
            _formationData = transform.parent.GetComponentInChildren<Formation.IShape>();
        }
        void Update()
        {
            _sensors = new();
            for (int i = 0; i < _sensorCount; i++)
            {
                float angle = i * 360f / _sensorCount + transform.rotation.z * Mathf.Deg2Rad;
                _sensors.Add(BoxSensor(angle));
            }
        }
        Vector3 Center
        {
            get {
                Vector3 offset = transform.up;
                offset.x *= _formationData.OffsetFromUnit.y;
                offset.y *= _formationData.OffsetFromUnit.y; 
                return transform.position + offset; }
        }
        public Vector2 SensorDirection(RaycastHit2D hit)
        {
            float angle = Sensors.FindIndex(x=>x== hit) * 360f / _sensorCount + transform.rotation.z * Mathf.Deg2Rad;
            return Quaternion.AngleAxis(angle, transform.forward) * transform.up;
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
            for (int i = 0; i < _sensorCount; i++)
            {
                float angle = i * 360f / _sensorCount + transform.rotation.z * Mathf.Deg2Rad;
                DrawBoxSensor(angle);
            }
        }

        
        #endregion
    }
}