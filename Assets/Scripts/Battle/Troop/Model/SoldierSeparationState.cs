using System;
using UnityEngine;

// Separation force applied to a soldier by soldiers of other troops, rebuilt every frame by the combat world.
[Serializable]
public sealed class SoldierSeparationState
{
    [SerializeField] Vector2 _external;

    public Vector2 External => _external;

    public void Add(Vector2 force) => _external += force;

    public void Clear() => _external = Vector2.zero;
}