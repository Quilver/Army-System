using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class ChargeSizer : MonoBehaviour
{
    UnitR unit;
    [SerializeField]
    List<UnitR> unitRList;
    public bool UnitAhead
    {
        get
        {
            return unitRList.Count> 0;
        }
    }
    public List<UnitR> Enemies
    {
        get
        {
            var enemies = new List<UnitR>();
            foreach (var unit in unitRList)
            {
                if(Battle.Instance.unitArmy[this.unit].EnemyUnits.Contains(unit))
                    enemies.Add(unit);
            }
            return enemies;
        }
    }
    void Start()
    {
        unit = GetComponentInParent<UnitR>();
        unitRList = new List<UnitR>();
        GetComponent<SpriteRenderer>().enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        SetBox();
    }
    Vector3 Midpoint
    {
        get
        {
            float x = -(unit.Movement.UnitWidth % 2 - 1) / 2f;
            float y = (unit.Movement.Ranks - 1) / 2f;
            return new Vector3(x, y);
        }
    }
    Vector3 Offset(float y)
    {
        return new Vector3(-(unit.Movement.UnitWidth % 2 - 1) / 2f, y);
    }
    void SetBox()
    {
        if (unit.ModelsRemaining == 0) return;
        //get values
        float angle = Angle;//unit.Movement.position.Rotation;
        Vector3 size = GetSize(unit.Movement.UnitWidth * unit.ModelSize.x);//new(unit.Movement.UnitWidth, 1 + Midpoint.y/2);
        Vector2 position = MidPoint(unit.Movement.UnitWidth, unit.Movement.Ranks, angle);
        //Vector2 rotatedOffset = Quaternion.Euler(0, 0, angle) * Offset(size.y);
        Vector2 pos = unit.LeadModelPosition;
        //set value
        transform.position = position + pos;
        transform.localScale = size;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        UnitR unit = collision.GetComponentInParent<UnitR>();
        if (unit == null) { return; }
        if(this.unit != unit)
        {
            unitRList.Add(unit);
            Notifications.StartFight(this.unit, unit);
            //Battle.Instance.CreateCombat(this.unit, unit);
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        UnitR unit = collision.GetComponentInParent<UnitR>();
        if (unit == null) { return; }
        if (this.unit != unit)
        {
            unitRList.Remove(unit);
            Notifications.EndFight(this.unit, unit);
            //Battle.Instance.EndCombat(this.unit, unit);
        }
    }
    public List<UnitInterface> TargetsAt(PositionR position)
    {
        List<UnitInterface> targets = new();
        float angle = position.Rotation;
        Vector3 size = GetSize(unit.Movement.UnitWidth * unit.ModelSize.x);
        Vector2 rotatedPos = MidPoint(unit.Movement.UnitWidth, unit.Movement.Ranks, angle);
        Vector2 pos = position.Location + rotatedPos;
        var collisions = Physics2D.OverlapBoxAll(pos, size, angle, 1 << 6);
        foreach (var coll in collisions)
        {
            targets.Add(GetComponentInParent<UnitR>());
        }
        return targets;
    }
    #region Helper functions
    const float meleeRange = 1.0f;
    Vector2 GetSize(float width)
    {
        return new(width, meleeRange);
    }
    Vector2 MidPoint(int width, int ranks, float angle)
    {
        Vector2 offset = MidPoint(width, ranks);
        offset = Quaternion.Euler(0, 0, angle) * offset;
        return offset;
    }
    Vector2 MidPoint(int width, int ranks)
    {
        float xOffset = -(width % 2 - 1) / 2f * unit.ModelSize.x;
        float yOffset = meleeRange/2 + 0.5f;
        return new(xOffset, yOffset);
    }
    float Angle
    {
        get
        {
            if (unit.ModelsRemaining <= 1)
                return unit.Movement.position.Rotation;
            Vector3 center = unit.LeadModelPosition;
            Vector3 right = unit.RightMostModelPosition - center;

            return Vector2.SignedAngle(Vector2.up, right) + 90; //Vector3.Angle(Vector3.zero, right);
        }
    }
    #endregion
}
