using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
//www[UnityEditor.InitializeOnLoad]
//[ExecuteInEditMode]
public class ChargeSizer : MonoBehaviour
{
    UnitR unit;
    [SerializeField]
    List<UnitR> enemies;
    // Start is called before the first frame update
    void Start()
    {
        //UnityEditor.EditorApplication.update += SetBoxEditorVersion;
        unit = GetComponentInParent<UnitR>();
        enemies = new List<UnitR>();
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
            enemies.Add(unit);
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        UnitR unit = collision.GetComponentInParent<UnitR>();
        if (unit == null) { return; }
        if (this.unit != unit)
        {
           // enemies.Remove(unit);
        }
    }
}
