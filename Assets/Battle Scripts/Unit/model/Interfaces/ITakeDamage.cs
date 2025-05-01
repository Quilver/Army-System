using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ModelComponents
{
    public abstract class ITakeDamage: MonoBehaviour
    {
        public abstract void TakeDamage(float damage, Transform attacker);
        public abstract void TakeDamage(float damage);

    }
}