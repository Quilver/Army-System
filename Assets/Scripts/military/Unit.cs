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
    public bool inCombat;
    private void Start()
    {
        if(army == null)
        {
            Debug.LogError("Unit not intialised");
            gameObject.SetActive(false);
        }
        SetRotation();
        create();
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
        //movement ai
        movementAI = new UnitMovementAI(this);
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
        UpdateMovement();
        updateCollider();
        UpdateVisual();
    }
    public void order(SelectionData section)
    {
        if (section == null) { return; }
        else if ((section as Tile).unit != null)
        {
            movementAI.getRoute((section as Tile).unit);
        }
        else if (section.GetType() == typeof(Tile))
        {
            movementAI.getRoute(section as Tile);
        }

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
            return;
        }
        else
        {
            int count = models.Count - numberOfDeaths;
            //print(gameObject.name + " is being reduced");
            //print(gameObject.name + " Starts with: " + models.Count + " number of deaths: " + numberOfDeaths + " and " + count + " models left");
            for (int i = models.Count - 1; i > count; i--)
            {
                models[i].Die();
                models.RemoveAt(i);
            }
        }
        updateCollider();
    }
    void OnDestroy()
    {
        if (models != null)
        {
            foreach (Model model in models)
            {
                Destroy(model);
            }
        }
    }

    #region Melee
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Regiment")// && col.gameObject.GetComponent<Unit>() != null)
        {
            Unit u = collision.gameObject.GetComponent<Unit>() as Unit;
            KeyValuePair<Unit, Unit> pa = new KeyValuePair<Unit, Unit>(this, u);
            inCombat = true;
            Master.Instance.combats.Add(new Combats(this, u));
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

    public int RankBonus { get { return UnitSize / UnitWidth; } }

    #endregion
    #region Pathfinding
    [SerializeField]
    CardinalDirections facing;
    //public Vector2 direction;
    public Vector2 CardinalDirection
    {
        get
        {
            switch (facing)
            {
                case CardinalDirections.N:
                    return new Vector2(0, 1);
                case CardinalDirections.NE:
                    return new Vector2(1, 1);
                case CardinalDirections.NW:
                    return new Vector2(-1, 1);
                case CardinalDirections.W:
                    return new Vector2(-1, 0);
                case CardinalDirections.SW:
                    return new Vector2(-1, -1);
                case CardinalDirections.S:
                    return new Vector2(0, -1);
                case CardinalDirections.SE:
                    return new Vector2(1, -1);
                case CardinalDirections.E:
                    return new Vector2(1, 0);
                default:
                    Debug.LogError("Unrecognised direction");
                    return new Vector2(0, 0);
            }
        }
        set
        {
            if (new Vector2(0, 1) == value) facing = CardinalDirections.N;
            else if (new Vector2(1, 1) == value) facing = CardinalDirections.NE;
            else if (new Vector2(-1, 1) == value) facing = CardinalDirections.NW;

            else if (new Vector2(-1, -1) == value) facing = CardinalDirections.SW;
            else if (new Vector2(0, -1) == value) facing = CardinalDirections.S;
            else if (new Vector2(1, -1) == value) facing = CardinalDirections.SE;

            else if (new Vector2(1, 0) == value) facing = CardinalDirections.E;
            else if (new Vector2(-1, 0) == value) facing = CardinalDirections.W;
        }
    }
    Vector2 destination;
    Tile wayPoint;
    UnitMovementAI movementAI;
    [SerializeField]
    UnitMoveState moveState;
    public void SetRotation()
    {
        if (facing == CardinalDirections.N) transform.rotation = Quaternion.Euler(new Vector3(0, 0, 180));
        else if (facing == CardinalDirections.NW) transform.rotation = Quaternion.Euler(new Vector3(0, 0, -135));
        else if (facing == CardinalDirections.NE) transform.rotation = Quaternion.Euler(new Vector3(0, 0, 135));
        else if (facing == CardinalDirections.W) transform.rotation = Quaternion.Euler(new Vector3(0, 0, -90));
        else if (facing == CardinalDirections.E) transform.rotation = Quaternion.Euler(new Vector3(0, 0, 90));
        else if (facing == CardinalDirections.S) transform.rotation = Quaternion.Euler(new Vector3(0, 0, 0));
        else if (facing == CardinalDirections.SW) transform.rotation = Quaternion.Euler(new Vector3(0, 0, -45));
        else if (facing == CardinalDirections.SE) transform.rotation = Quaternion.Euler(new Vector3(0, 0, 45));
    }
    public Vector2 CenterPoint { get { return models[models.Count / 2].position; } }
    void UpdateMovement()
    {
        if (inCombat || movementAI.route == null) return;
        movementAI.validateRoute();
        if (movementAI.route != null && movementAI.route.waypoints != null && movementAI.route.waypoints.Count > 0 && wayPoint == null)
        {
            Path<Node<Tile>> altWayPoint = movementAI.followPath();
            wayPoint = altWayPoint.state.data;
            destination = new Vector2(wayPoint.position.x, wayPoint.position.y);
            Vector2 altDir = destination - (Vector2)transform.position;
            if (altWayPoint.state.direction.normalized != altDir.normalized)
            {
                moveState = UnitMoveState.Strafe;
            }
            else
            {
                moveState= UnitMoveState.Wheel;
            }
        }
        switch (moveState)
        {
            case UnitMoveState.Walk:
                Walk();
                break;
            case UnitMoveState.Strafe:
                Strafe();
                break;
            case UnitMoveState.Wheel:
                Wheel();
                break;
            default:
                wayPoint= null;
                break;
        }
    }
    void Wheel()
    {
        Vector2 vect = wayPoint.position - (Vector2)transform.position;
        if (Mathf.Abs(vect.x) > 1) vect /= Mathf.Abs(vect.x);
        if (vect != CardinalDirection)
        {
            
            CardinalDirection = vect;
            foreach (Model model in models)
            {
                model.setRotation(vect);
            }
            return;
        }
        else { }
        foreach (Model model in models)
        {
            if (!model.move())
            {
                return;
            }
        }
        moveState = UnitMoveState.Walk;
    }
    void Walk()
    {
        foreach (Model model in models)
        {
            if (model.move())
            {
                model.moveOrder(destination);
            }
        }
        transform.position = Vector2.MoveTowards(transform.position, destination, type.Stats.MoveSpeed * Time.deltaTime);
        if (transform.position.x == destination.x && transform.position.y == destination.y)
        {
            moveState = UnitMoveState.Idle;
        }
    }

    void Strafe()
    {
        foreach (Model model in models)
        {
            if (model.move())
            {
                model.moveOrder(destination);
            }
        }
        transform.position = Vector2.MoveTowards(transform.position, destination, type.Stats.MoveSpeed * Time.deltaTime);// * StrafeSpeed);
        if (transform.position.x == destination.x && transform.position.y == destination.y)
        {
            moveState = UnitMoveState.Idle;
        }
    }
    public bool canMove(Node<Tile> node)
    {
        foreach (Model model in models)
        {
            if (!model.MoveEdge(node))
            {
                return false;
            }
        }
        return true;
    }
    public bool canMove(Edge<Tile> node)
    {
        foreach (Model model in models)
        {
            if (!model.MoveEdge(node))
            {
                return false;
            }
        }
        return true;
    }
    #endregion

}