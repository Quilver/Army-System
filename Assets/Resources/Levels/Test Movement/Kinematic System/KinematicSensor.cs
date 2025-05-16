using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
namespace MovementSystem
{
    class KinematicSensor : MonoBehaviour, ISensors
    {
        IMovementData _moveData;
        Vector2 Velocity
        {
            get
            {
                if( _moveData == null ) _moveData=GetComponent<IMovementData>();
                return _moveData.Velocity;
            }
        }
        [SerializeField, Range(1, 10)]
        float _sensorLength;
        [SerializeField, Range(30, 60)]
        float _sideWhiskerAngle;
        public float SensorLength => _sensorLength;
        [SerializeField]
        LayerMask SensorLayerMask;
        [SerializeField, Range(12, 36)] int _rayCount;
        [SerializeField, Range(2, 5)] float _rayLength;
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


        RaycastHit2D _forwardSensor, _rWhisker, _lWhisker;
        public RaycastHit2D ForwardSensor => _forwardSensor;
        public RaycastHit2D RightWhisker => _rWhisker;
        public RaycastHit2D LeftWhisker => _lWhisker;
        Formation.IShape _formationData;
        Formation.IShape FormationData
        {
            get
            {
                if(_formationData == null) _formationData = transform.parent.GetComponentInChildren<Formation.IShape>();
                return _formationData;
            }
        }
        Dictionary<RaycastHit2D, Vector2> _raycastDirection;

        void FixedUpdate()
        {
            UpdateRaycasts();
        }
        void UpdateRaycasts()
        {
            _raycastDirection = new();
            if(_sensors == null || _sensors.Count != _rayCount)_sensors = new(new RaycastHit2D[_rayCount]);
            if (Velocity.sqrMagnitude == 0) { }
            else
            {
                float forwardAngle = Vector2.Angle(Vector2.right, Velocity.normalized) * Mathf.Deg2Rad;
                _forwardSensor=BoxSensorVelocity(forwardAngle);
                _rWhisker= BoxSensorVelocity(forwardAngle + _sideWhiskerAngle);
                _lWhisker= BoxSensorVelocity(forwardAngle + 360 - _sideWhiskerAngle);
            }
            for (int i = 0; i < _rayCount; i++)
            {
                float angle = i * 360f / (float)_rayCount;
                var dir = Quaternion.AngleAxis(angle, transform.forward) * Vector2.up;
                _sensors[i] = Physics2D.Raycast(Center, dir, _rayLength, SensorLayerMask);
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
                offset.x *= FormationData.OffsetFromUnit.y;
                offset.y *= FormationData.OffsetFromUnit.y;
                return transform.position + offset;
            }
        }
        RaycastHit2D BoxSensorVelocity(float angle)
        {

            Vector3 direction = Quaternion.AngleAxis(angle, transform.forward) * Velocity.normalized;
            Vector3 size = FormationData.SizeOfFormation;
            return Physics2D.BoxCast(Center, size, angle, direction, SensorLength - FormationData.SizeOfFormation.y / 2, SensorLayerMask);


        }
        #region Gizmo Display
        [SerializeField]
        bool _drawGizmo;
        void DrawBoxSensorVelocity(float angle)
        {
            var angleAdjusted = Quaternion.AngleAxis(angle, transform.forward);
            var perpendicular = Vector2.Perpendicular(Velocity.normalized) * 0.5f * FormationData.SizeOfFormation.x;
            var right = Center - angleAdjusted * perpendicular;
            var left = Center + angleAdjusted * perpendicular;
            Vector3 direction = angleAdjusted * Velocity.normalized;
            float length = SensorLength;

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
        private void OnDrawGizmos()
        {
            if (!_drawGizmo) return;
            if (Velocity.magnitude == 0) { }
            else
            {
                float forwardAngle = Vector2.Angle(Vector2.right, Velocity.normalized) * Mathf.Deg2Rad;
                DrawBoxSensorVelocity(0 + forwardAngle);
                DrawBoxSensorVelocity(_sideWhiskerAngle + forwardAngle);
                DrawBoxSensorVelocity(360 - _sideWhiskerAngle + forwardAngle);
            }
            if(Sensors != null)
                foreach (var sensor in Sensors) if(sensor)Gizmos.DrawLine(Center, sensor.point);
        }
        #endregion
    }
}
