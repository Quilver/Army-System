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
            return Battle.Instance.player.GetComponent<ClusterMap>().NearestCluster(_unit.LeadModelPosition);
        }
    }
    UnitR _unit;
    // Start is called before the first frame update
    void Start()
    {
        _unit = GetComponentInParent<UnitR>();

    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawSphere(TargetPosition, 0.3f);
    }
    // Update is called once per frame
    void Update()
    {
        if (_unit.State == UnitState.Idle)
            _unit.Movement.MoveTo(TargetPosition);
    }
}