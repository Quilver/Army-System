using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

public class UnitR : MonoBehaviour
{
	#region Properties
	[SerializeField]
	UnitStatsR stats;
	[SerializeField]
	UnitState state;
	[SerializeField]
	UnitPositionR movement;
    public UnitPositionR Movement
    {
        get { return movement; }
    }
	public List<ModelR> models;
    [SerializeField] GameObject _modelPrefab;
    [SerializeField]
    int _startingUnitSize;
    #endregion
    #region Initialise
    private void Start()
    {
        transform.position = new Vector3((int)transform.position.x, (int)transform.position.y, (int)transform.position.z);
        PopulateModels();
    }
    void PopulateModels()
    {
        if (movement.numberOfFiles > _startingUnitSize) movement.numberOfFiles = _startingUnitSize;
        models = new List<ModelR>();
        int xOffset = movement.UnitWidth / 2;
        int yOffset = (int)Mathf.Ceil((_startingUnitSize * 1.0f) / movement.UnitWidth);
        GameObject parent = new GameObject(gameObject.name);
        for (int y = 0; y < yOffset; y++)
        {
            for (int x = -xOffset; x < movement.UnitWidth - xOffset; x++)
            {
                if (_startingUnitSize <= 0) { break; }
                Vector2 offset = new Vector2(x, y);
                var model = Instantiate(type.Visual, parent.transform).GetComponent<Model>();
                model.Init((int)transform.position.x, (int)transform.position.y, offset, this, models.Count - 1);
                models.Add(model);
                count--;
            }
        }
    }
    #endregion
    #region Update

    #endregion
    #region Movement
    public void MoveTo(Vector2Int position)
    {

    }
    public void MoveTo(UnitR target)
    {

    }
    #endregion
    #region Combat and death

    #endregion
}
