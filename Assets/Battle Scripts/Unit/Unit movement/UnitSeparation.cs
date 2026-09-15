using System.Collections.Generic;
using UnityEngine;

namespace MovementSystem
{
    /// <summary>
    /// Resolves overlap between unit formation boxes. Soldiers remain free to
    /// collide with opposing units, while units themselves cannot interpenetrate.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UnitSeparation : MonoBehaviour
    {
        [SerializeField, Min(0f)] float contactSlop = 0.02f;
        [SerializeField, Min(0.01f)] float correctionRate = 8f;
        [SerializeField, Min(0.01f)] float maximumCorrectionSpeed = 4f;

        static readonly List<UnitSeparation> Active = new();
        IUnit _unit;
        UnitBody _body;
        Collider2D _formationCollider;

        void Awake()
        {
            _unit = GetComponentInParent<IUnit>();
            _body = GetComponent<UnitBody>();
            if (_unit != null)
            {
                Rigidbody2D unitBody = _unit.GetComponent<Rigidbody2D>();
                foreach (var collider in _unit.GetComponentsInChildren<Collider2D>())
                {
                    if (collider is BoxCollider2D && collider.attachedRigidbody == unitBody)
                    {
                        _formationCollider = collider;
                        break;
                    }
                }
            }
        }

        void OnEnable()
        {
            if (!Active.Contains(this))
                Active.Add(this);
        }

        void OnDisable() => Active.Remove(this);

        void FixedUpdate()
        {
            if (_formationCollider == null || !isActiveAndEnabled)
                return;

            for (int i = 0; i < Active.Count; i++)
            {
                UnitSeparation other = Active[i];
                if (other == null || other == this || other._formationCollider == null)
                    continue;
                if (GetInstanceID() > other.GetInstanceID())
                    continue;

                ResolvePair(other);
            }
        }

        void ResolvePair(UnitSeparation other)
        {
            ColliderDistance2D distance = _formationCollider.Distance(other._formationCollider);
            float penetration = -distance.distance;
            if (penetration <= contactSlop)
                return;

            Vector2 normal = distance.normal;
            Vector2 centerDelta = _formationCollider.bounds.center - other._formationCollider.bounds.center;
            if (Vector2.Dot(normal, centerDelta) < 0f)
                normal = -normal;
            if (normal.sqrMagnitude < 0.0001f)
                normal = centerDelta.sqrMagnitude > 0.0001f ? centerDelta.normalized : Vector2.right;

            float correction = Mathf.Min(
                (penetration - contactSlop) * correctionRate,
                maximumCorrectionSpeed);

            if (IsEngaged(other))
            {
                _body?.AddSeparationVelocity(normal * correction * 0.5f);
                other._body?.AddSeparationVelocity(-normal * correction * 0.5f);
                return;
            }

            UnitSeparation yielding = SelectYieldingUnit(other);
            yielding._body?.AddSeparationVelocity((yielding == this ? normal : -normal) * correction);
        }

        bool IsEngaged(UnitSeparation other) =>
            (_unit != null && _unit.InMelee) || (other._unit != null && other._unit.InMelee);

        UnitSeparation SelectYieldingUnit(UnitSeparation other)
        {
            float thisSpeed = _body == null ? 0f : _body.CurrentVelocity.sqrMagnitude;
            float otherSpeed = other._body == null ? 0f : other._body.CurrentVelocity.sqrMagnitude;
            if (thisSpeed > otherSpeed + 0.0001f)
                return this;
            if (otherSpeed > thisSpeed + 0.0001f)
                return other;
            return GetInstanceID() > other.GetInstanceID() ? this : other;
        }

        public static void IgnoreCollisionsWithin(Transform root)
        {
            if (root == null)
                return;

            Collider2D[] colliders = root.GetComponentsInChildren<Collider2D>();
            for (int i = 0; i < colliders.Length; i++)
            {
                for (int j = i + 1; j < colliders.Length; j++)
                    Physics2D.IgnoreCollision(colliders[i], colliders[j], true);
            }
        }
    }
}
