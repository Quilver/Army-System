using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ModelComponents
{
    public interface IMeleeTargeter
    {
        public bool InCombat { get; }
        public List<ITakeDamage> Targets { get; }
        public ITakeDamage Target { get; }
    }
}