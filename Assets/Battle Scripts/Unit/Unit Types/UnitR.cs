using JetBrains.Annotations;
using StatSystem;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;

public class UnitR : MonoBehaviour, UnitInterface
{
    #region Properties
    //[SerializeField]
	//UnitStatsR stats;
    [SerializeField]
    StatSystem.UnitStats unitStats;
    public StatSystem.UnitStats UnitStats => unitStats;
    
    [SerializeField]
    UnitState _state;
	public UnitState State
    {
        get { return _state; }
        set {
            if(_state == UnitState.Fighting && value == UnitState.Idle)
                foreach(var model in models) model.STOPPED = false;
            _state = value; 
        }
    }
	[SerializeField]
	UnitPositionR movement;
    Weapon weapon;
    public Weapon Melee { 
        get { return weapon; }
    }
    public UnitPositionR Movement
    {
        get { 
            return movement; 
        }
    }
	List<ModelR> models;
    [SerializeField] 
    GameObject _modelPrefab;
    #endregion
    #region Model information
    ModelR _modelBase;
    public Vector2 ModelSize
    {
        get { 
            if(_modelBase == null) _modelBase = _modelPrefab.GetComponent<ModelR>();
            return _modelBase.ModelSize;
        }
    }
    public Vector3 LeadModelPosition
    {
        get
        {
            return models[0].transform.position;
        }
    }
    public Vector3 RightMostModelPosition
    {
        get
        {
            if(models.Count== 0) return Vector3.zero;
            else if(Movement.UnitWidth == 1) return models[0].transform.position;
            else if(Movement.UnitWidth % 2 == 0) return models[Movement.UnitWidth - 1].transform.position;
            else return models[Movement.UnitWidth-2].transform.position;
        }
    }
    public Vector3 LeftMostModelPosition
    {
        get
        {
            if (models.Count == 0) return Vector3.zero;
            else if (models.Count == 1) return models[0].transform.position;
            else if (Movement.UnitWidth % 2 == 0) return models[Movement.UnitWidth - 2].transform.position;
            else return models[Movement.UnitWidth - 1].transform.position;
        }
    }
    public bool ModelsAreMoving
    {
        get
        {
            for (int i = models.Count - 1; i >= 0; i--)
            {
                if (models[i].Moving)
                    return true;
            }
            return false;
        }
    }
    public int ModelsRemaining
    {
        get {
            if (models == null) return 0;
            return models.Count; 
        }
    }
    #endregion
    #region Initialise
    private void Start()
    {
        _state = UnitState.Idle;
        var size = GetComponent<UnitSize>();
        movement.Init(this, size.UnitWidth);
        transform.position = new Vector3((int)transform.position.x, (int)transform.position.y, (int)transform.position.z);
        PopulateModels(size.StartingSize, size.UnitWidth);
        _maxUnitSize =size.StartingSize;
        Destroy(size);
        weapon= new Weapon(this);
    }
    void PopulateModels(int size, int width)
    {
        int _startingUnitSize = size;
        if (width > _startingUnitSize) width = _startingUnitSize;
        models = new List<ModelR>();
        int yOffset = (int)Mathf.Ceil((_startingUnitSize * 1.0f) / width);
        GameObject parent = new(gameObject.name);
        for (int y = 0; y < yOffset; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int Xoffset = x/2;
                if (x % 2 == 0) Xoffset = -Xoffset;
                else Xoffset += 1;
                Vector2Int offset = new(Xoffset, y);
                var model = Instantiate(_modelPrefab, parent.transform).GetComponent<ModelR>();
                model.Init(offset, this, models.Count - 1);
                models.Add(model);
            }
        }
    }
    #endregion
    #region Update
    private void Update()
    {
        if(State == UnitState.Fighting)
            weapon.UpdateCombat();
        movement.UpdateMovement();
        if (models.Count == 0) Die();
    }
    #endregion
    #region Combat and death
    int _maxUnitSize;
    public void STOP() {
        foreach (var model in models) model.STOPPED = true;
    }
    public bool Wounded
    {
        get
        {
            return models.Count <= _maxUnitSize/2;
        }
    }
    public void TakeDamage(int damage)
    {
        for (int i = 0; i < damage; i++)
        {
            if (models.Count == 0)
            {
                //Die();
                return;
            }
            Destroy(models[models.Count - 1].gameObject);
            models.RemoveAt(models.Count - 1);

        }
    }
    void Die()
    {
        if (this == null) return;
        Notifications.Died(this);
        //Battle.Instance.EndCombat(this);
        Destroy(this.gameObject);
    }
    #endregion
    public override string ToString()
    {
        return UnitStats.ToString();
    }

    
}
