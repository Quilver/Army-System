using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#region Enemy Behaviours
public class Aggroed : MonoBehaviour
{
    UnitR _unit;
    [SerializeField]
    float thinkingSpeed;
    [SerializeField]
    int AggroRange;
    float _time = 0;
    public UnitR enemy;
    // Start is called before the first frame update
    void Start()
    {
        _unit = GetComponent<UnitR>();

    }
    int DistanceFromUnit(UnitR unit)
    {
        var pos = unit.Movement.position.Location - _unit.Movement.position.Location;
        return Math.Max(Math.Abs(pos.x), Math.Abs(pos.y));
    }
    UnitR GetNearestUnit()
    {
        int distance =int.MaxValue;
        UnitR closestUnit=null;
        foreach (var enemy in Battle.Instance.unitArmy[_unit].Enemies)
        {
            int dist = DistanceFromUnit(enemy);
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
        if (DistanceFromUnit(enemy) < AggroRange)
        {
            _unit.Movement.MoveTo(enemy.Movement.position.Location);
        }
        else
            enemy = null;
    }
}
#endregion
