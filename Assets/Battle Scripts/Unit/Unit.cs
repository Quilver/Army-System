using StatSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using StatSystem.Refactor;

class Unit : IUnit
{
    [SerializeField]
    StatSystem.Refactor.IUnitStatBlock _stats;
    public override IUnitStatBlock Stats { 
        get => _stats;
        set => _stats = value; 
    }
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
                if(models==null)return false;
                foreach (var model in models)
                    model.GetComponentInChildren<ModelComponents.IMeleeTargeter>().ChangedCombat+=ModelsFighting;
                _MeleeInit = true;
            }
            RefreshModelsFighting();
            return _modelsFighting > 0;
        }
    }

    

    void ModelsFighting(bool enteredMelee)
    {
        RefreshModelsFighting();
    }

    void RefreshModelsFighting()
    {
        var models = GetComponentInChildren<Formation.IFormationData>()?.Models;
        if (models == null) return;

        int previousCount = _modelsFighting;
        _modelsFighting = 0;
        foreach (var model in models)
        {
            if (model == null) continue;
            var targeter = model.GetComponentInChildren<ModelComponents.IMeleeTargeter>();
            if (targeter != null && targeter.InCombat)
                _modelsFighting++;
        }

        if (previousCount == 0 && _modelsFighting > 0) Melee(true);
        else if (previousCount > 0 && _modelsFighting == 0) Melee(false);
    }
    [SerializeField]
    bool _inMelee;
    private void Update()
    {
        _inMelee=InMelee;
    }
}
