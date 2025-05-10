using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UI.CanvasScaler;
namespace ModelComponents
{
    class MeleeTargeter : MonoBehaviour, IMeleeTargeter
    {
        [SerializeField]
        UnitData _unitData;
        [SerializeField]
        List<ITakeDamage> _inCombatWith;
        [SerializeField, Range(0.5f, 3)]
        float MaxRange;
        public bool InCombat => Targets.Count > 0;

        public List<ITakeDamage> Targets
        {
            get {
                _inCombatWith.RemoveAll(item => item == null);
                _inCombatWith.RemoveAll(enemy => Vector2.Distance(transform.position, enemy.transform.position) > MaxRange);
                return _inCombatWith;
            }
        }

        public ITakeDamage Target => Targets[0];
        void Start()
        {
            if(_unitData == null)
                _unitData = GetComponentInParent<UnitData>();
            _inCombatWith = new();
        }
        private void OnTriggerEnter2D(Collider2D collision)
        {
            var collUnit = collision.gameObject.GetComponent<ITakeDamage>();
            if(collUnit == null)return;
            var unitData = collUnit.GetComponent<IUnitData>();
            if (unitData == null || unitData.Unit == _unitData.Unit) return;
            if (_inCombatWith.Contains(collUnit) || !Battle.Instance.Enemies(_unitData.Unit, unitData.Unit)) return;
            _inCombatWith.Add(collUnit);
        }
    }
}