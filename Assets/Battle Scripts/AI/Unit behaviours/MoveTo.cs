using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#region Enemy Behaviours
public class MoveTo : MonoBehaviour
{
    [SerializeField]
    Vector2 targetPosition;
    UnitBase _unit;
    // Start is called before the first frame update
    void Start()
    {
        _unit= GetComponentInParent<UnitBase>();
        
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawSphere(targetPosition, 0.3f);
    }
    // Update is called once per frame
    void Update()
    {
        if(_unit.State == UnitState.Idle)
            _unit.Movement.MoveTo(targetPosition);
    }
}
#endregion

