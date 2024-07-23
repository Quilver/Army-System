using System.Collections;
using System.Collections.Generic;
using UnitMovement;
using UnityEngine;

public abstract class UnitBase: MonoBehaviour
{
    public abstract StatSystem.UnitStats UnitStats { get; }
    public abstract UnitState State { get; set; }
    public abstract IMovement Movement { get; }
    public abstract bool Wounded { get; }
    public abstract void TakeDamage(int damage);
    #region Models
    protected List<ModelR> Models { get; set; }
    protected virtual void InstantiateModels(int size, int width, bool relative = false)
    {
        int _startingUnitSize = size;
        if (width > _startingUnitSize) width = _startingUnitSize;
        Models = new List<ModelR>();
        int yOffset = (int)Mathf.Ceil((_startingUnitSize * 1.0f) / width);
        GameObject parent = new("Models");
        parent.transform.SetParent(transform);
        for (int y = 0; y < yOffset; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int Xoffset = x / 2;
                if (x % 2 == 0) Xoffset = -Xoffset;
                else Xoffset += 1;
                Vector2Int offset = new(Xoffset, y);
                var model = Instantiate(UnitStats.UnitPrefab, parent.transform).GetComponent<ModelR>();
                model.Init(offset, this, Models.Count - 1);
                Models.Add(model);
            }
        }
    }
    public int ModelsRemaining
    {
        get
        {
            if (Models == null) return 0;
            return Models.Count;
        }
    }
    public bool ModelsAreMoving
    {
        get
        {
            for (int i = Models.Count - 1; i >= 0; i--)
            {
                if (Models[i].Moving)
                    return true;
            }
            return false;
        }
    }
    public Vector2 ModelSize => UnitStats.UnitPrefab.GetComponent<ModelR>().ModelSize;
    public Vector3 LeadModelPosition {
        get
        {
            if (Models == null || Models.Count == 0)
                return Movement.Location;
            return Models[0].transform.position;
        }
    }
    public float ROffset
    {
        get
        {
            if (Models.Count == 0) return 0;
            else if (Movement.Files == 1) return Models[0].offset.x;
            else if (Movement.Files % 2 == 0) return Models[Movement.Files - 1].offset.x;
            else return Models[Movement.Files - 2].offset.x;
        }
    }
    public Vector3 RightMostModelPosition
    {
        get
        {
            if (Models.Count == 0) return Vector3.zero;
            else if (Movement.Files == 1) return Models[0].transform.position;
            else if (Movement.Files % 2 == 0) return Models[Movement.Files - 1].transform.position;
            else return Models[Movement.Files - 2].transform.position;
        }
    }
    public float LOffset
    {
        get
        {
            if (Models.Count == 0) return 0;
            else if (Models.Count == 1) return Models[0].offset.x;
            else if (Movement.Files % 2 == 0) return Models[Movement.Files - 2].offset.x;
            else return Models[Movement.Files - 1].offset.x;
        }
    }
    public Vector3 LeftMostModelPosition
    {
        get
        {
            if (Models.Count == 0) return Vector3.zero;
            else if (Models.Count == 1) return Models[0].transform.position;
            else if (Movement.Files % 2 == 0) return Models[Movement.Files - 2].transform.position;
            else return Models[Movement.Files - 1].transform.position;
        }
    }
    #endregion
}
