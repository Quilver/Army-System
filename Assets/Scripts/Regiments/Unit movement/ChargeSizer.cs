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
                if(Master.Instance.unitArmy[this.unit].Enemies.Contains(unit))
                    enemies.Add(unit);
            }
            return enemies;
        }
    }
    void Start()
    {
        unit = GetComponentInParent<UnitR>();
        unitRList = new List<UnitR>();
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
        if (unit.models.Count == 0) return;
        //get values
        float angle = unit.Movement.position.Rotation;
        Vector3 size = GetSize(unit.Movement.UnitWidth);//new(unit.Movement.UnitWidth, 1 + Midpoint.y/2);
        Vector2 position = MidPoint(unit.Movement.UnitWidth, unit.Movement.Ranks, angle);
        //Vector2 rotatedOffset = Quaternion.Euler(0, 0, angle) * Offset(size.y);
        Vector2 pos = unit.models[0].transform.position;
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
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        UnitR unit = collision.GetComponentInParent<UnitR>();
        if (unit == null) { return; }
        if (this.unit != unit)
        {
            unitRList.Remove(unit);
        }
    }
    public List<UnitR> TargetsAt(PositionR position)
    {
        List<UnitR> targets = new();
        float angle = position.Rotation;
        Vector3 size = GetSize(unit.Movement.UnitWidth);
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
    const float meleeRange = 1;
    Vector2 GetSize(int width)
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
        float xOffset = -(width % 2 - 1) / 2f;
        float yOffset = meleeRange;
        return new(xOffset, yOffset);
    }
    #endregion
}
