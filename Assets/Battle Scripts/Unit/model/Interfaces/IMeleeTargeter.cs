using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ModelComponents
{
    public interface IMeleeTargeter
    {
        //Fires if entered(true) or exited(false) combat
        public event System.Action<bool> ChangedCombat;
        public bool InCombat { get; }
        public List<ITakeDamage> Targets { get; }
        public ITakeDamage Target { get; }
    }
}