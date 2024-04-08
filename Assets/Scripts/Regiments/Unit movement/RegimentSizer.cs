using System.Collections;
using System.Collections.Generic;
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
        unit = GetComponent<UnitR>();
        int ranks = (int)Mathf.Round((Size * 1.0f) / width);
        float angle = GetComponentInParent<UnitR>().Movement.position.Rotation;
        Vector3 size = new(width, ranks);
        if (angle % 10 != 0)
        {
            size *= 1.25f;
            size.x += 0.25f;
        }
        Vector2 midpoint = new(-(width % 2 - 1) / 2f, -(ranks - 1) / 2f);
        transform.localPosition = midpoint;
        transform.localScale = size;
        transform.parent.rotation= Quaternion.Euler(0, 0, angle);
    }
    void SetBox()
    {
        //get values
        float angle = unit.Movement.position.Rotation;
        Vector3 size = new(unit.Movement.UnitWidth, unit.Movement.Ranks);
        if (angle % 10 != 0)
        {
            size *= 1.25f;
            size.x += 0.25f;
        }
        Vector2 rotatedOffset = Quaternion.Euler(0, 0, angle) * Midpoint;
        //set value
        transform.position = rotatedOffset + unit.Movement.position.Location;
        transform.localScale = size;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }
}
