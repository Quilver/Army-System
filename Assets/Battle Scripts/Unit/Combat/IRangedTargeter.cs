using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Shooting
{
    public interface IRangedTargeter
    {
        public List<Transform> ValidTargets { get; }
        public Transform Target { get; }
    }
}
