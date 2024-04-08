using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveTo : MonoBehaviour
{
    [SerializeField]
    Vector2Int targetPosition;
    UnitR _unit;
    // Start is called before the first frame update
    void Start()
    {
        _unit= GetComponent<UnitR>();
        
    }

    // Update is called once per frame
    void Update()
    {
        if(_unit.state == UnitState.Idle)
            _unit.Movement.MoveTo(targetPosition);
    }
}
