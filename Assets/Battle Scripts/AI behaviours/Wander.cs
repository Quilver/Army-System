using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#region Enemy Behaviours
public class Wander : MonoBehaviour
{
    UnitR unit;
    Vector2 center;
    [SerializeField, Range(5, 20)]
    int wanderDistance;
    // Start is called before the first frame update
    void Start()
    {
        unit = GetComponent<UnitR>();
        center= transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (unit.State == UnitState.Idle)
            unit.Movement.MoveTo(RandomMove());
    }
    Vector2Int RandomMove()
    {
        int x =Random.Range(-wanderDistance, wanderDistance+1) + (int)center.x;
        int y = Random.Range(-wanderDistance, wanderDistance + 1) + (int)center.y;
        return new Vector2Int(x, y);
    }
}
#endregion

