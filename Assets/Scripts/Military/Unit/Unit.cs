using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Unit : MonoBehaviour
{
    [SerializeField] Army army;
    public Army Controller { get { return army; } }
    public List<Model> models;
    [SerializeField] UnitType type;
    public UnitStats Stats { get { return type.Stats; } }    
    [SerializeField] int UnitSize;
    [SerializeField] int UnitWidth;
    public UnitState unitState;
    [SerializeField]
    public UnitMovementHandler unitMovementHandler;
    public Vector2 CardinalDirection
    {
        get { return unitMovementHandler.CardinalDirection; }
        set { unitMovementHandler.CardinalDirection = value; }
    }
    private void Start()
    {
        if(army == null)
        {
            Debug.LogError("Unit not intialised");
            gameObject.SetActive(false);
        }
        create();
        unitMovementHandler.Init(this, models);
        unitMovementHandler.SetRotation();
        
        //visual.SetActive(false);
    }
    
    public void create()
    {
        gameObject.name = type.UnitName;
        //movement
        transform.position = new Vector3((int)transform.position.x, (int)transform.position.y, (int)transform.position.z);
        Map.Instance.getTile((int)transform.position.x, (int)transform.position.y).unit = this;
        //model initialisation
        populateModels();
        updateCollider();
        
    }
    [SerializeField] GameObject visual;
    public void UpdateVisual()
    {
        
        visual.transform.localScale = unitSize;
        visual.transform.localPosition = offset;
    }
    Vector2 unitSize { get { return new Vector2(UnitWidth, Mathf.Ceil(UnitSize / (UnitWidth + 0f))); } }
    Vector2 offset { get { return new Vector2((UnitWidth % 2 - 1) / 2f, Mathf.Ceil(UnitSize / (UnitWidth * 2f)) - 0.5f); } }
    public void updateCollider()
    {
        if(models.Count== 0) return;    
        UnitSize = models.Count;
        if (UnitSize < UnitWidth)
        {
            UnitWidth = UnitSize;
        }
        BoxCollider2D box = gameObject.GetComponent<BoxCollider2D>();
        box.size = unitSize;
        box.offset = offset;
        transform.rotation = Quaternion.AngleAxis(AngleBetweenVector2(transform.position, models[0].position) - 180, Vector3.forward);
    }
    public static float AngleBetweenVector2(Vector2 vec1, Vector2 vec2)
    {
        Vector2 diference = vec2 - vec1;
        float sign = (vec2.y < vec1.y) ? -1.0f : 1.0f;
        return Vector2.Angle(Vector2.right, diference) * sign;
    }
    void populateModels()
    {
        //populate unit with models
        models = new List<Model>();
        int count = UnitSize;
        if (UnitWidth > UnitSize) { UnitWidth = UnitSize; }
        int xOffset = UnitWidth / 2;
        int yOffset = (int)Mathf.Ceil((UnitSize * 1.0f) / UnitWidth);
        GameObject parent = new GameObject(type.name);
        //Add models to unit
        for (int y = 0; y < yOffset; y++)
        {
            for (int x = -xOffset; x < UnitWidth - xOffset; x++)
            {
                if (count <= 0) { break; }
                Vector2 offset = new Vector2(x, y);
                var model = Instantiate(type.Visual, parent.transform).GetComponent<Model>();
                model.Init((int)transform.position.x, (int)transform.position.y, offset, this, models.Count-1);
                models.Add(model);
                count--;
            }
        }
    }
    public string getDetails()
    {
        string details = type.UnitName + " at X:" + (int)transform.position.x + ", Y:" + (int)transform.position.y + "";
        details += "\nNumber of models: " + UnitSize;
        details += "\nMovement speed " + type.Stats.MoveSpeed;
        details += "\nAttack Speed " + type.Stats.Speed;
        details += "\nWeapon Skill " + type.Stats.WeaponSkill;
        details += "\nStrength " + type.Stats.AttackStrength;
        details += "\nDefence " + type.Stats.Defence;
        return details;
    }
    public void Update()
    {
        unitMovementHandler.UpdateMovement();
        UpdateCombat();
        updateCollider();
        UpdateVisual();
    }
    public void order(SelectionData section)
    {
        if (section == null) { return; }
        else if ((section as Tile).unit != null)
        {
            unitMovementHandler.movementAI.getRoute((section as Tile).unit);
        }
        else if (section.GetType() == typeof(Tile))
        {
            unitMovementHandler.movementAI.getRoute(section as Tile);
        }

    }
    
    

    #region Melee
    float _attackTime = float.MinValue;
    public void EndCombat()
    {
        unitState = UnitState.Idle;
        _attackTime = float.MinValue;
    }
    void UpdateCombat()
    {
        if (unitState != UnitState.Fighting || _attackTime > Time.time) return;
        Master.Instance.combats.MakeAttacks(this);
        _attackTime= Time.time + 1 / Stats.Speed;
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Regiment")// && col.gameObject.GetComponent<Unit>() != null)
        {
            Unit u = collision.gameObject.GetComponent<Unit>() as Unit;
            KeyValuePair<Unit, Unit> pa = new KeyValuePair<Unit, Unit>(this, u);
            //inCombat = true;
            unitState = UnitState.Fighting;
            Master.Instance.combats.AddCombat(this, u);
        }
    }
    public bool adjacentTile(Vector2 position)
    {
        bool adjacent = false;
        foreach (Model model in models)
        {
            if (1 >= Mathf.Abs(model.position.x - position.x) && 1 >= Mathf.Abs(model.position.y - position.y))
            {
                adjacent = true;
                break;
            }
        }
        return adjacent;
    }
    public void deaths(int numberOfDeaths)
    {
        if (models == null || models.Count == 0) { return; }
        if (models.Count <= numberOfDeaths)
        {
            //print(gameObject.name + " has been removed");
            foreach (Model model in models)
            {
                model.Die();
            }
            models = null;
            Destroy(this);
            return;
        }
        else
        {
            int count = models.Count - numberOfDeaths;
            for (int i = models.Count - 1; i > count; i--)
            {
                models[i].Die();
                models.RemoveAt(i);
            }
        }
        updateCollider();
    }
    public int RankBonus { get { return UnitSize / UnitWidth; } }
    void OnDestroy()
    {
        Master.Instance.combats.BreakCombat(this);
        if (models != null)
        {
            foreach (Model model in models)
            {
                Destroy(model);
            }
        }
    }
    #endregion
}