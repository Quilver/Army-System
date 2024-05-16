using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#region Enemy Behaviours
public class Aggroed : MonoBehaviour
{
    UnitInterface _unit;
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
    float DistanceFromUnit(UnitR unit)
    {
        var pos = unit.Movement.position.Location - _unit.Movement.position.Location;
        return Mathf.Max(Mathf.Abs(pos.x), Mathf.Abs(pos.y));
    }
    UnitR GetNearestUnit()
    {
        float distance =float.MaxValue;
        UnitR closestUnit=null;
        foreach (var enemy in Battle.Instance.unitArmy[_unit].EnemyUnits)
        {
            float dist = DistanceFromUnit((UnitR)enemy);
            if(dist < distance)
            {
                distance = dist;
                closestUnit = enemy as UnitR;
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
            _unit.Movement.MoveTo(enemy.Movement.position.Location);
        }
        else
            enemy = null;
    }
}
#endregion
