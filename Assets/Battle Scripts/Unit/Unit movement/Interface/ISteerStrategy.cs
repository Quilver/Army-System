using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SteeringSystem
{
    public abstract class ISteerStrategy : MonoBehaviour
    {
        [SerializeField]
        List<ISteeringBehaviour> _behaviours;
        // Start is called before the first frame update
        private void OnEnable()=>EnableEvents();
        private void OnDisable()=>DisableEvents();
        protected abstract void EnableEvents();
        protected abstract void DisableEvents();
        protected virtual void Enter()
        {
            foreach(var behaviour in _behaviours) behaviour.enabled=true;
        }
        protected virtual void Exit()
        {
            foreach (var behaviour in _behaviours) behaviour.enabled = false;
        }
    }
}
