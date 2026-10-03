using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ModelComponents
{
    public abstract class IDeath : MonoBehaviour
    {
        [SerializeField]
        public UnityEvent Dead;
        public abstract void Die();
        
    }
}