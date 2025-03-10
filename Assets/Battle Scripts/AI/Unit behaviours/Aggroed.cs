using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#region Enemy Behaviours
public class Aggroed : MonoBehaviour
{
    UnitTemplate _unit;
    [SerializeField, Range(0.3f, 3)]
    float thinkingSpeed;
    [SerializeField, Range(2, 12)]
    float AggroRange;
    float _time = 0;
    public UnitTemplate enemy;
    // Start is called before the first frame update
    void Start()
    {
        _unit = GetComponentInParent<UnitTemplate>();

    }
    float DistanceFromUnit(UnitTemplate unit)
    {
        var pos = unit.transform.position - _unit.transform.position;
        return Mathf.Max(Mathf.Abs(pos.x), Mathf.Abs(pos.y));
    }
    UnitTemplate GetNearestUnit()
    {
        float distance =float.MaxValue;
        UnitTemplate closestUnit = null;
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
            _unit.MoveTo(enemy.transform);
        }
        else
            enemy = null;
    }
    [SerializeField]
    bool Gizmo;
    private void OnDrawGizmos()
    {
        if (!enabled || !Gizmo) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, AggroRange);
    }
}
#endregion
