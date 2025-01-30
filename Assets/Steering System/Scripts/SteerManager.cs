using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//Handles Setup of unit and movement controls
public class SteerManager : MonoBehaviour
{
    #region Fields
    [SerializeField]
    GameObject ModelPrefab;
    [SerializeField, Range(1, 4)]
    float ModelSize;
    [SerializeField, Range(1, 32)]
    int modelCount;
    [SerializeField, Range(1, 8)]
    int Width;
    #endregion
    #region Public properties
    public Vector2 Location => transform.position;
    public float Rotation => transform.eulerAngles.z;
    public int Files => Width;
    public int Ranks => (modelCount % Width > 0) ? modelCount / Width + 1 : modelCount / Width;
    #endregion
    #region Commands
    public void MoveTo(Vector2 location)
    {
        throw new System.NotImplementedException();
    }

    public void MoveTo(UnitTemplate unit)
    {
        throw new System.NotImplementedException();
    }
    #endregion
}