using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;

public class RegimentSizer : MonoBehaviour
{
    UnitR unit;
    // Start is called before the first frame update
    void Start()
    {
        unit = GetComponentInParent<UnitR>();
        
    }
    // Update is called once per frame
    void Update()
    {
        transform.parent.position = Vector3.zero;
        SetBox();
    }
    Vector3 Midpoint
    {
        get {
            float x = -(unit.Movement.UnitWidth % 2 - 1) / 2f;
            float y = -(unit.Movement.Ranks - 1) / 2f;
            return new Vector3(x,y); 
        }
    }


    public void SetBox(int width, int Size)
    {
        float angle = GetComponentInParent<UnitR>().Movement.position.Rotation;
        int ranks = Mathf.CeilToInt((Size *1f)/width);
        Vector3 size = GetSize(width, ranks);

        Vector2 midpoint = MidPoint(width, (int)size.y);//new(-(width % 2 - 1) / 2f, -(ranks - 1) / 2f);
        transform.localPosition = midpoint;
        transform.localScale = size;
        transform.parent.rotation= Quaternion.Euler(0, 0, angle);
    }
    public bool CanBeOn(PositionR pos, float avoidBy, int width, int ranks, UnitR target = null)
    {
        float angle = pos.Rotation;
        Vector2 size = GetSize(width, ranks, avoidBy);
        var midPoint = MidPoint(new Vector2(width, ranks), angle);
        //checks against other units
        var overlaps = Physics2D.OverlapBoxAll(midPoint + pos.Location, size, pos.Rotation, 1 << 6);
        foreach (var collider2D in overlaps)
        {
            var clipping = collider2D.GetComponentInParent<UnitR>();
            if (clipping != unit && clipping != target)
                return true;
        }
        overlaps = Physics2D.OverlapBoxAll(midPoint + pos.Location, size, pos.Rotation, 1 << 8);
        //checks against terrain
        if (overlaps.Length != 0)
            return true;
        return false;
    }
    void SetBox()
    {
        if (unit.models.Count == 0) return;
        //get values
        float angle = unit.Movement.position.Rotation;
        Vector3 size = GetSize(unit.Movement.UnitWidth, unit.Movement.Ranks);
        var midpoint = MidPoint(size, angle);
        //Vector2 rotatedOffset = Quaternion.Euler(0, 0, angle) * Midpoint;
        Vector2 pos = unit.models[0].transform.position;
        //set value
        transform.position = midpoint+ pos;// unit.Movement.position.Location;
        transform.localScale = size;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }
    #region HelperFunctions
    Vector2 GetSize(int width, int ranks)
    {
        return new(width,ranks);
    }
    Vector2 GetSize(int width, int size, float avoidBy)
    {
        int ranks = Mathf.CeilToInt(size / (float)width);
        Vector2 Size = new(width,ranks);
        Size.x += avoidBy;
        Size.y -= 0.1f;
        return Size;
    }
    Vector2 MidPoint(Vector2 size, float angle)
    {
        Vector2 offset = MidPoint((int)size.x, (int)size.y);
        offset = Quaternion.Euler(0,0, angle) * offset;
        return offset;
    }
    Vector2 MidPoint(int width, int ranks)
    {
        float xOffset = -(width % 2 - 1) / 2f;
        float yOffset = -(ranks - 1) / 2f;
        return new(xOffset, yOffset);
    }
    #endregion
}
