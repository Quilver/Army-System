using System.Collections.Generic;
using UnityEngine;

namespace MovementSystem
{
    /// <summary>Aggregates soldier displacement without moving the unit transform.</summary>
    public sealed class UnitCohesion : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] float displacementSmoothing = 0.35f;
        [SerializeField, Min(0f)] float displacementThreshold = 0.08f;
        [SerializeField, Min(0.01f)] float giveGroundRate = 2f;
        [SerializeField, Range(0.1f, 1f)] float minimumCohesion = 0.35f;
        [SerializeField, Min(0.01f)] float fullDisplacement = 1f;

        readonly Dictionary<MonoBehaviour, Vector2> _slotDisplacements = new();
        readonly List<MonoBehaviour> _staleSlots = new();
        Vector2 _averageDisplacement;

        public Vector2 AverageDisplacement => _averageDisplacement;
        public Vector2 GiveGround
        {
            get
            {
                float magnitude = _averageDisplacement.magnitude;
                if (magnitude <= displacementThreshold)
                    return Vector2.zero;
                return Vector2.ClampMagnitude(
                    _averageDisplacement.normalized * (magnitude - displacementThreshold) * giveGroundRate,
                    giveGroundRate);
            }
        }
        public float CohesionFactor
        {
            get
            {
                float displacement = Mathf.Max(0f, _averageDisplacement.magnitude - displacementThreshold);
                return Mathf.Lerp(1f, minimumCohesion, Mathf.Clamp01(displacement / fullDisplacement));
            }
        }

        public void SetSlotDisplacement(MonoBehaviour slot, Vector2 displacement)
        {
            if (slot != null)
                _slotDisplacements[slot] = displacement;
        }

        public void RemoveSlot(MonoBehaviour slot)
        {
            if (slot != null)
                _slotDisplacements.Remove(slot);
        }

        void FixedUpdate()
        {
            Vector2 total = Vector2.zero;
            _staleSlots.Clear();
            foreach (var slot in _slotDisplacements)
            {
                if (slot.Key == null)
                    _staleSlots.Add(slot.Key);
                else
                    total += slot.Value;
            }

            foreach (var slot in _staleSlots)
                _slotDisplacements.Remove(slot);

            Vector2 target = _slotDisplacements.Count == 0 ? Vector2.zero : total / _slotDisplacements.Count;
            float blend = 1f - Mathf.Exp(-Time.fixedDeltaTime / displacementSmoothing);
            _averageDisplacement = Vector2.Lerp(_averageDisplacement, target, blend);
        }
    }
}
