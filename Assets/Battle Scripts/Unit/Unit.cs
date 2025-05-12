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
    int _modelsFighting;
    bool _MeleeInit;
    public override bool InMelee {
        get
        {
            if (!_MeleeInit) {
                var models = GetComponentInChildren<Formation.IFormationData>().Models;
                foreach (var model in models)
                    model.GetComponentInChildren<ModelComponents.IMeleeTargeter>().ChangedCombat+=ModelsFighting;
                _MeleeInit = true;
            }
            return _modelsFighting > 0;
        }
    }
    void ModelsFighting(bool enteredMelee)
    {
        if (enteredMelee) _modelsFighting++;
        else _modelsFighting--;
        if(enteredMelee && _modelsFighting == 1) Melee(true);
        else if(_modelsFighting == 0) Melee(false);
    }
    [SerializeField]
    bool _inMelee;
    private void Update()
    {
        _inMelee=InMelee;
    }
}
