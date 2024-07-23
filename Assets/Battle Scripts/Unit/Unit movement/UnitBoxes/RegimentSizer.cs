using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;

public class RegimentSizer : MonoBehaviour
{
    UnitBase unit;
    [SerializeField]
    List<GameObject> enemies;
    [SerializeField]
    bool relative;
    [SerializeField]
    bool debug;
    // Start is called before the first frame update
    void Start()
    {
        unit = GetComponentInParent<UnitBase>();
        enemies = new();
        if(!debug)
            GetComponent<SpriteRenderer>().enabled= false;
    }
    // Update is called once per frame
    void Update()
    {
        if(!relative)
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
        float angle = GetComponentInParent<UnitBase>().Movement.Rotation;
        Vector2 modelSize = GetComponentInParent<UnitBase>().ModelSize;
        int ranks = Mathf.CeilToInt((Size * 1f) / width);
        Vector3 size = GetSize(width, ranks, modelSize);

        Vector2 midpoint = MidPoint(width, (int)size.y, modelSize);//new(-(width % 2 - 1) / 2f, -(ranks - 1) / 2f);
        transform.localPosition = midpoint;
        transform.localScale = size;
        transform.parent.rotation = Quaternion.Euler(0, 0, angle);
    }
    public bool CanBeOn(PositionR pos, float avoidBy, int width, int ranks, UnitBase target = null)
    {
        float angle = pos.Rotation;
        Vector2 size = GetSize(width, ranks, avoidBy, unit.ModelSize);
        var midPoint = MidPoint(new Vector2(width, ranks), angle, unit.ModelSize);
        //checks against other units
        var overlaps = Physics2D.OverlapBoxAll(midPoint + pos.Location, size, pos.Rotation, 1 << 6);
        foreach (var collider2D in overlaps)
        {
            var clipping = collider2D.GetComponentInParent<UnitBase>();
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
        Vector3 size = GetSize(unit.Movement.Files, unit.Movement.Ranks, unit.ModelSize);
        
        var midpoint = MidPoint(size, angle, unit.ModelSize);
        Vector2 pos = unit.LeadModelPosition;
        _size = midpoint;
        //set value
        if (relative) transform.position = midpoint;
        else
            transform.position = midpoint + pos;// unit.Movement.position.Location;
        transform.localScale = size;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }
    #endregion

    #region HelperFunctions
    Vector2 GetSize(float width, float ranks, Vector2 unitSize)
    {
        return new(width * unitSize.x ,ranks * unitSize.y);
    }
    Vector2 GetSize(float width, float size, float avoidBy, Vector2 unitSize)
    {
        int ranks = Mathf.CeilToInt(size / (float)width);
        Vector2 Size = GetSize(width,ranks, unitSize);
        Size.x += avoidBy;
        Size.y -= 0.1f;
        return Size;
    }
    Vector2 MidPoint(Vector2 size, float angle, Vector2 modelSize)
    {
        Vector2 offset = MidPoint(size.x, size.y, modelSize);
        offset = Quaternion.Euler(0,0, angle) * offset;
        return offset;
    }
    Vector2 MidPoint(float width, float ranks, Vector2 modelSize)
    {
        float xOffset = -(width % 2 - 1) / 2f;
        float yOffset = -(ranks - 1) / 2f;
        return new(xOffset * modelSize.x, yOffset);
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
