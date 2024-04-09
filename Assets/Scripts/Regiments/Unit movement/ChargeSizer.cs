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
    Vector3 offset(float y)
    {
        return new Vector3(-(unit.Movement.UnitWidth % 2 - 1) / 2f, y);
    }
    void SetBoxEditorVersion()
    {
        //get values
        float angle = unit.Movement.position.Rotation;
        Vector3 size = new(unit.Movement.UnitWidth, 1 + Midpoint.y / 2);
        if (angle % 10 != 0)
        {
            size *= 1.25f;
            size.x += 0.25f;
        }
        Vector2 rotatedOffset = Quaternion.Euler(0, 0, angle) * offset(size.y);
        //set value
        transform.position = rotatedOffset + unit.Movement.position.Location;
        transform.localScale = size;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }
    void SetBox()
    {
        //get values
        float angle = unit.Movement.position.Rotation;
        Vector3 size = new(unit.Movement.UnitWidth, 1 + Midpoint.y/2);
        if (angle % 10 != 0)
        {
            size *= 1.25f;
            size.x += 0.25f;
        }
        Vector2 rotatedOffset = Quaternion.Euler(0, 0, angle) * offset(size.y);
        //set value
        transform.position = rotatedOffset + unit.Movement.position.Location;
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
}
