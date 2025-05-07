using StatSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

class Unit : IUnit
{
    [SerializeField]
    RegimentStats _stats;
    public override RegimentStats Stats => _stats;
    
    [SerializeField]
    UnitState _state;

    
    public override UnitState State {
        get
        {
            return _state;
        }
        set
        {
            //Check if change is valid
            if (State == value) return;

            //Change state
            ChangeState(value);
            _state = value;
        }
    }

}
