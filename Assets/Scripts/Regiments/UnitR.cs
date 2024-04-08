using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

public class UnitR : MonoBehaviour
{
	#region Properties
	[SerializeField]
	public UnitStatsR stats;
	[SerializeField]
	public UnitState state;
	[SerializeField]
	UnitPositionR movement;
    public Weapon weapon;
    public UnitPositionR Movement
    {
        get { 
            return movement; 
        }
    }
	public List<ModelR> models;
    [SerializeField] GameObject _modelPrefab;
    [SerializeField]
    #endregion
    #region Initialise
    private void Start()
    {
        var size = GetComponent<UnitSize>();
        //movement.unitSetter= this;
        movement.Init(this, size.UnitWidth);
        //movement.position.Location = new Vector2Int((int)transform.position.x, (int)transform.position.y);
        transform.position = new Vector3((int)transform.position.x, (int)transform.position.y, (int)transform.position.z);
        PopulateModels(size.StartingSize, size.UnitWidth);
        Destroy(size);
        time = 0;
        weapon= new Weapon(this);
    }
    void PopulateModels(int size, int width)
    {
        int _startingUnitSize = size;
        if (width > _startingUnitSize) width = _startingUnitSize;
        models = new List<ModelR>();
        int xOffset = width / 2;
        int yOffset = (int)Mathf.Ceil((_startingUnitSize * 1.0f) / width);
        GameObject parent = new GameObject(gameObject.name);
        for (int y = 0; y < yOffset; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int Xoffset = x/2;
                if (x % 2 == 0) Xoffset = -Xoffset;
                else Xoffset += 1;
                Vector2Int offset = new Vector2Int(Xoffset, y);
                var model = Instantiate(_modelPrefab, parent.transform).GetComponent<ModelR>();
                model.Init(offset, this, models.Count - 1);
                models.Add(model);
            }
        }
    }
    #endregion
    #region Update
    float time;
    private void Update()
    {
        if (state == UnitState.Fighting)
            Fighting();
        movement.UpdateMovement();
    }
    #endregion
    #region Combat and death
    public void Fighting()
    {
        time -= Time.deltaTime;
        if (time < 0)
        {
            time = 10f/stats.AttackSpeed;
            Master.Instance.MakeAttack(this);
        }
    }
    public void Die(int deaths)
    {
        for (int i = 0; i < deaths; i++)
        {
            if (models.Count == 0)
            {
                Die();
                return;
            }
            Destroy(models[models.Count - 1].gameObject);
            models.RemoveAt(models.Count - 1);
            
        }
    }
    void Die()
    {
        if (this == null) return;
        Master.Instance.RemoveCombat(this, true);
        Destroy(this.gameObject);
    }
    #endregion
}
