using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelR : MonoBehaviour
{
    #region Properties
    UnitR unit;
    Vector2Int offset;
    #endregion
    // Start is called before the first frame update
    public void Init(Vector2Int offset, UnitR owner, int index)
    {
        this.unit = owner;
        this.offset=offset
    }
}
