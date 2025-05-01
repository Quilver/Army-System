using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Shooting
{
    public interface IRangedTargeter
    {
        public List<UnitTemplate> ValidTargets { get; }
        public UnitTemplate Target { get; }
    }
}
