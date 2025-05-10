using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
namespace SteeringSystem.Reaction
{
    [RequireComponent (typeof(IUnit))]
    public class Flee : MonoBehaviour
    {
        IUnit _unit;
        Rigidbody2D _body;
        ArmyData _army;
        ArmyData _Army
        {
            get
            {
                if (_army == null)_army = GetComponentInParent<ArmyData>();
                return _army;
            }
        }
        // Start is called before the first frame update
        void Start()
        {
            _body = GetComponent<Rigidbody2D>();
            _unit = GetComponent<IUnit>();
            _unit.StateChanged += React;
            enabled = false;
        }
        void React(UnitState state)
        {
            if (state != UnitState.Fleeing) {
                enabled = false;
                return;
            }
            enabled = true;
            _body.AddForce(FleeDirection() * _forceMultiple * 30);
        }
        private void Update()
        {
            FleeFromEnemy();
        }
        [SerializeField, Range(50, 300)]
        float _forceMultiple;
        void FleeFromEnemy()
        {
            _body.AddForce(FleeDirection() * Time.deltaTime * _forceMultiple);

        }
        Vector2 FleeDirection()
        {
            if(NearestEnemy() == null)return Vector2.zero;
            return (transform.position - NearestEnemy().position).normalized;
        }
        Transform NearestEnemy()
        {
            if(_Army == null) return null;
            float bestDistance = 1000;
            Transform nearestEnemy = null;
            var enemies = _Army.Enemies;
            foreach(var enemy in enemies) 
                if(Vector2.Distance(transform.position, enemy.transform.position) < bestDistance)
                {
                    nearestEnemy = enemy.transform;
                    bestDistance = Vector2.Distance(transform.position, enemy.transform.position);
                }
            return nearestEnemy;
        }

        [SerializeField]
        bool DrawGizmo;
        private void OnDrawGizmos()
        {
            if(!DrawGizmo) return;
            Gizmos.DrawRay(transform.position, FleeDirection());
        }
    }
}