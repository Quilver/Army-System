using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flee : SteeringBehaviour
{
    [SerializeField, Range(0.1f, 1)]
    float _priority;
    public override float priority => _priority;
    [SerializeField, Range(3, 10)]
    float maxDistance;
    List<Transform> NearbyEnemyUnits
    {
        get
        {
            List<Transform> nearbyUnits = new();
            var coll = Physics2D.OverlapCircleAll(parent.transform.position, maxDistance);
            if(coll != null) 
                foreach(var hit in coll)
                {
                    var hitUnit = hit.gameObject.GetComponent<UnitTemplate>();
                    if(hitUnit==null) continue;
                    if (hitUnit.GetComponentInParent<Army>() != parent.GetComponentInParent<Army>()) nearbyUnits.Add(hit.transform);
                }
            return nearbyUnits;
        }
    }
    bool shoveFlag=false;
    private void OnEnable()
    {
        shoveFlag = true;
    }
    private void OnDisable()
    {
        if (GetComponentInParent<SteerTowards>() == null) return;
    }
    float WeightedPriority(float distance)
    {
        return priority * Mathf.InverseLerp(maxDistance, 0, distance);
    }
    [SerializeField, Range(1, 3)]
    float FleeSpeedBonus = 2;
    public override Vector2 GetDirection()
    {
        parent.SetArrivalModifier(FleeSpeedBonus);
        if (shoveFlag)
        {
            Debug.Log(NearbyEnemyUnits.Count);
            
            Vector3 p1;
            if (NearestEnemy == null) p1 = parent.transform.position - parent.transform.up;
            else p1 = NearestEnemy.position;
            var dir = p1-parent.transform.position;
            parent.GetComponent<Rigidbody2D>().AddForce((dir).normalized * 1000);
            shoveFlag=false;
        }
        foreach (Transform unit in NearbyEnemyUnits)
        {
            parent.AddSteeringForce(-parent.Seek(unit.position), WeightedPriority(priority));
        }
        return Vector2.zero;
    }
    public Transform NearestEnemy
    {
        get
        {
            Transform enemy = null;
            float distance = float.MaxValue;
            foreach (Transform unit in NearbyEnemyUnits)
            {
                if(distance < Vector3.Distance(unit.position, parent.transform.position)) continue;
                enemy = unit;
                distance = Vector3.Distance(unit.position, parent.transform.position);
            }
            return enemy;
        }
    }
    public bool DrawGizmo;
    public void OnDrawGizmos()
    {
        if (!DrawGizmo || parent == null || !enabled) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(parent.transform.position, maxDistance);
    }
}
