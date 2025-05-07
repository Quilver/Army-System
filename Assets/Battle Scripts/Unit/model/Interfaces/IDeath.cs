using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ModelComponents
{
    public interface IDeath
    {
        public event System.Action Death;
        void Die();
        
    }
}