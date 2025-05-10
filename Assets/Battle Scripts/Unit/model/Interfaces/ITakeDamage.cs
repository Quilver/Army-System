using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ModelComponents
{
    public abstract class ITakeDamage: MonoBehaviour
    {
        public static event System.Action<IUnit, IUnit> kill;
        protected static void LogKill(IUnit attacker, IUnit victim)  
            =>kill?.Invoke(attacker, victim);
        
        public abstract void TakeDamage(float damage, Transform attacker);
        public abstract void TakeDamage(float damage);

    }
}