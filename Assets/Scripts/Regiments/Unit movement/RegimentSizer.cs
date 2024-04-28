using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;

public class RegimentSizer : MonoBehaviour
{
    UnitR unit;
    [SerializeField]
    List<GameObject> enemies;
    // Start is called before the first frame update
    void Start()
    {
        unit = GetComponentInParent<UnitR>();
        enemies = new();
    }
    // Update is called once per frame
    void Update()
    {
        transform.parent.position = Vector3.zero;
        SetBox();
    }
    public bool Clipping
    {
        get
        {
            return enemies.Count > 0;
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        var box = collision.gameObject.GetComponent<RegimentSizer>();
        if (box == null) return;
        enemies.Add(box.gameObject);
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        var box = collision.gameObject.GetComponent<RegimentSizer>();
        if (box == null) return;
        enemies.Remove(box.gameObject);
    }
    #region Box setter
    public void SetBox(int width, int Size)
    {
        float angle = GetComponentInParent<UnitR>().Movement.position.Rotation;
        int ranks = Mathf.CeilToInt((Size * 1f) / width);
        Vector3 size = GetSize(width, ranks);

        Vector2 midpoint = MidPoint(width, (int)size.y);//new(-(width % 2 - 1) / 2f, -(ranks - 1) / 2f);
        transform.localPosition = midpoint;
        transform.localScale = size;
        transform.parent.rotation = Quaternion.Euler(0, 0, angle);
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
    [SerializeField]
    Vector2 _size;
    void SetBox()
    {
        if (unit.ModelsRemaining == 0) return;
        //get values
        float angle = Angle; // unit.Movement.position.Rotation;
        Vector3 size = GetSize(unit.Movement.UnitWidth, unit.Movement.Ranks);
        
        var midpoint = MidPoint(size, angle);
        Vector2 pos = unit.LeadModelPosition;
        _size = midpoint;
        //set value
        transform.position = midpoint + pos;// unit.Movement.position.Location;
        transform.localScale = size;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }
    #endregion

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
    float Angle
    {
        get
        {
            if(unit.ModelsRemaining <= 1)
                return 0;
            Vector3 center = unit.LeadModelPosition;
            Vector3 right = unit.RightMostModelPosition - center;
            return Vector2.SignedAngle(Vector2.up, right) + 90; //Vector3.Angle(Vector3.zero, right);
        }
    }
    #endregion
}
