using InfluenceMap;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveTowardsEnemyCluster : MonoBehaviour
{
    Vector2 TargetPosition
    {
        get
        {
            return _clusterMap.NearestCluster(_unit.LeadModelPosition);
        }
    }
    UnitBase _unit;
    ClusterMap _clusterMap;
    // Start is called before the first frame update
    void Start()
    {
        _unit = GetComponentInParent<UnitBase>();
        _clusterMap = Battle.Instance.player.GetComponent<ClusterMap>();
    }

    private void OnDrawGizmosSelected()
    {
        if(_unit== null) return;
        Gizmos.DrawSphere(TargetPosition, 0.3f);
    }
    // Update is called once per frame
    void Update()
    {
        if(_unit== null) return;
        if (_unit.State != UnitState.Idle) return;
        if (_clusterMap.ClusterCount != 0) 
            _unit.Movement.MoveTo(TargetPosition);
    }
}