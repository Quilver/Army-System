using SoftBody;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UI.CanvasScaler;
namespace ModelComponents
{
    class MeleeTargeter : MonoBehaviour, IMeleeTargeter
    {
        Model model;
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
            model = GetComponentInParent<Model>();
            _inCombatWith = new();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            var collUnit = collision.gameObject.GetComponent<ITakeDamage>();
            if (collUnit == null || collision.transform == transform.parent) return;
            if (_inCombatWith.Contains(collUnit)) return;// || !Battle.Instance.Enemies(model.unit, )) return;
            _inCombatWith.Add(collUnit);
        }
    }
}