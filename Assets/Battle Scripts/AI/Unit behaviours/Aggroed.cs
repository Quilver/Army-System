using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#region Enemy Behaviours
public class Aggroed : MonoBehaviour
{
    UnitBase _unit;
    [SerializeField]
    float thinkingSpeed;
    [SerializeField]
    int AggroRange;
    float _time = 0;
    public UnitBase enemy;
    // Start is called before the first frame update
    void Start()
    {
        _unit = GetComponentInParent<UnitBase>();

    }
    float DistanceFromUnit(UnitBase unit)
    {
        var pos = unit.Movement.Location - _unit.Movement.Location;
        return Mathf.Max(Mathf.Abs(pos.x), Mathf.Abs(pos.y));
    }
    UnitBase GetNearestUnit()
    {
        float distance =float.MaxValue;
        UnitBase closestUnit =null;
        foreach (var enemy in Battle.Instance.unitArmy[_unit].EnemyUnits)
        {
            float dist = DistanceFromUnit(enemy);
            if(dist < distance)
            {
                distance = dist;
                closestUnit = enemy;
            }
        }
        return closestUnit;
    }
    // Update is called once per frame
    void Update()
    {
        _time += Time.deltaTime;
        if(_time > thinkingSpeed)
        {
            _time = 0;
            AggroLogic();
        }
    }
    void AggroLogic()
    {
        enemy = GetNearestUnit();
        if (enemy != null && DistanceFromUnit(enemy) < AggroRange)
        {
            _unit.Movement.MoveTo(enemy);
        }
        else
            enemy = null;
    }
}
#endregion
