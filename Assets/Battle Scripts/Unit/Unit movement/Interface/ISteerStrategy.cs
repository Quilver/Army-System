using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MovementSystem
{
    public abstract class ISteerStrategy : MonoBehaviour
    {
        ISteeringBehaviour[] _behaviours;
        protected virtual void Start ()
        {
            _behaviours = GetComponents<ISteeringBehaviour>();  
        }
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
