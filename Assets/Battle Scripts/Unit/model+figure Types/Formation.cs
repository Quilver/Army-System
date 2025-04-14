using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//Manages the shape of the unit
//Creates the unit's figure after deployment and stores them so they can be referenced
//Defaults as a rectangle
[RequireComponent(typeof(UnitTemplate))]
public abstract class Formation : MonoBehaviour
{
    #region Formation initialisation properties
    [Range(1, 4)]
    public float ModelSize;
    [Range(1, 8)]
    public int Width;
    public int ModelCount
    {
        get
        {
            if (models == null)
                return unit.Stats.ModelCount.CurrentStat;
            else
                return models.Count;
        }
    }
    #endregion
    #region other properties
    public List<SoftBody.Model> models;
    UnitTemplate unit;
    BoxCollider2D unitCollider;
    Rigidbody2D[] unitPins;
    #endregion
    #region Deployment
    void CreateUnit(UnitTemplate UselessData)
    {

    }
    #endregion
    #region Formation Positions
    public int Files
    {
        get
        {
            if (models == null || models.Count >= Width || models.Count == 0) return Width;
            return models.Count;
        }
    }
    public int Ranks
    {
        get
        {
            return (ModelCount % Files > 0) ? ModelCount / Files + 1 : ModelCount / Files;
        }
    }
    Vector2 UnitSize
    {
        get
        {
            return new Vector2(Files, Ranks) * ModelSize / 2;
        }
    }
    Vector2 unitOffset
    {
        get
        {
            return new Vector2(transform.position.x, transform.position.y - (Ranks - 1) * ModelSize / 4);
        }
    }
    Vector2 GetModelPos(int modelIndex)
    {
        float x = modelIndex % Files;
        if (x % 2 == 0) x = -x / 2;
        else x = x / 2 + 0.5f;
        if (Files % 2 == 0) x -= 0.5f;


        int y = (modelIndex / Files);
        Vector3 offset = new Vector3(x, -y) * ModelSize / 2;
        offset = Quaternion.AngleAxis(transform.eulerAngles.z, Vector3.forward) * offset;
        return transform.position + offset;
    }
    #endregion
    #region Reform after death

    #endregion
    
}
