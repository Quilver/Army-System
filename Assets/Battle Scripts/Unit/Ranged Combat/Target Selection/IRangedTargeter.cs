using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Shooting
{
    public interface IRangedTargeter
    {
        //public event System.Action<Vector2> ChangedTargets;
        public enum DefaultTarget
        {
            None,
            Nearest
        }

        public List<Transform> ValidTargets { get; }
        public Transform Target { get; }
    }
}
