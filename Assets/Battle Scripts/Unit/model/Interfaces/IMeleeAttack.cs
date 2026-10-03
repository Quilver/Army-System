using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ModelComponents
{
    public abstract class IMeleeAttack : MonoBehaviour
    {
        public event System.Action Strike;
        protected void MakeStrike()=>Strike?.Invoke();
    }
}
