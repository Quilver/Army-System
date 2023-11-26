using System.Collections;
using System.Collections.Generic;
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
    #region Movement
    public Vector2 position, direction;
    Vector2 destination;
    public bool rotating, moving, strafing;
    Tile wayPoint;
    UnitMovementAI movementAI;
    #endregion
    public bool inCombat;
    private void Start()
    {
        create();
    }
    public void create()
    {
        gameObject.name = type.UnitName;
        //movement
        position = new Vector2(transform.position.x, transform.position.y);
        Debug.Log(Map.Instance);
        Map.Instance.getTile((int)position.x, (int)position.y).unit = this;
        rotating = false;
        moving = false;
        strafing = false;
        //model initialisation
        populateModels();
        updateCollider();
        //movement ai
        movementAI = new UnitMovementAI(this);
    }
    public void updateCollider()
    {
        if(models.Count== 0) return;    
        UnitSize = models.Count;
        if (UnitSize < UnitWidth)
        {
            UnitWidth = UnitSize;
        }
        BoxCollider2D box = gameObject.GetComponent<BoxCollider2D>();
        box.size = new Vector2(UnitWidth, Mathf.Ceil(UnitSize / (UnitWidth + 0f)));
        box.offset = new Vector2(0, Mathf.Ceil(UnitSize / (UnitWidth * 2f)) - 0.5f);
        gameObject.transform.position = position;
        gameObject.transform.rotation = Quaternion.AngleAxis(CombatEngine.AngleBetweenVector2(position, models[0].position) - 180, Vector3.forward);
    }
    void populateModels()
    {
        //populate unit with models
        models = new List<Model>();
        int count = UnitSize;
        if (UnitWidth > UnitSize) { UnitWidth = UnitSize; }
        int xOffset = UnitWidth / 2;
        int yOffset = (int)Mathf.Ceil((UnitSize * 1.0f) / UnitWidth);
        //Add models to unit
        for (int y = 0; y < yOffset; y++)
        {
            for (int x = -xOffset; x < UnitWidth - xOffset; x++)
            {
                if (count <= 0) { break; }
                Vector2 offset = new Vector2(x, y);
                var model = Instantiate(type.Visual).GetComponent<Model>();
                model.Init((int)position.x, (int)position.y, offset, this);
                models.Add(model);
                count--;
            }
        }
    }
    public string getDetails()
    {
        string details = type.UnitName + " at X:" + (int)position.x + ", Y:" + (int)position.y + "";
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
        movementUpdate();
        updateCollider();
    }
    public void order(SelectionData section)
    {
        //Debug.Log("Recieved order");
        if (section == null) { return; }
        else if (section as Unit != null)
        {
            Debug.Log("Moving to unit");
            movementAI.getRoute(section as Unit);
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
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Regiment")// && col.gameObject.GetComponent<Unit>() != null)
        {
            print("Collided with unit");
            Unit u = collision.gameObject.GetComponent<Unit>() as Unit;
            KeyValuePair<Unit, Unit> pa = new KeyValuePair<Unit, Unit>(this, u);
            if (!Master.Instance.battles.ContainsKey(new KeyValuePair<Unit, Unit>(u, this)) && !Master.Instance.battles.ContainsKey(pa))
            {
                //print("Entered combat");
                inCombat = true;
                Master.Instance.battles.Add(pa, gameObject.AddComponent<CombatEngine>().create(this, u) as CombatEngine);
            }
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
    public CombatEngine.AngleOfAttack numberOfContacts(GameObject obj)
    {
        CombatEngine.fight fight = CombatEngine.fight.Flank;
        bool setAngle = false;
        int contacts = 0;
        foreach (Model model in models)
        {
            if (obj.GetComponent<BoxCollider2D>().IsTouching(model.gameObject.GetComponent<BoxCollider2D>()))
            {
                contacts++;
                Vector2 pos = (Vector2)model.gameObject.transform.position;
                pos.x = Mathf.Round(pos.x);
                pos.y = Mathf.Round(pos.y);
                if (!setAngle)
                {
                    string names = "";
                    names += (pos + direction) + " ";
                    if (Map.Instance.getTile(pos + direction).unit != null)
                    {
                        names += "Front " + Map.Instance.getTile(pos + direction).unit.gameObject.name;
                    }
                    names += ", ";
                    names += (pos - direction) + " ";
                    if (Map.Instance.getTile(pos + direction).unit != null)
                    {
                        names += "Rear " + Map.Instance.getTile(pos - direction).unit.gameObject.name;
                    }
                    print(gameObject.name + ": " + pos + " (" + names + ") " + obj.name);
                }
                if (setAngle)
                {
                    continue;
                }
                else if (Map.Instance.getTile(pos + direction).unit != null &&
                    Map.Instance.getTile(pos + direction).unit.gameObject == obj)
                {
                    setAngle = true;
                    fight = CombatEngine.fight.Front;
                }
                else if (Map.Instance.getTile(pos - direction).unit != null &&
                    Map.Instance.getTile(pos - direction).unit.gameObject == obj)
                {
                    setAngle = true;
                    fight = CombatEngine.fight.Front;
                }
                else
                {
                    setAngle = true;
                    fight = CombatEngine.fight.Flank;
                }
            }
        }
        //print(gameObject.name + " " + direction + " " + fight);

        return new CombatEngine.AngleOfAttack { contacts = contacts, f = fight };
    }
    public Vector2 getCenterPoint()
    {
        return models[models.Count / 2].position;
    }
    public int rankBonus()
    {
        int rank = UnitSize / UnitWidth;
        if (rank > 3) { rank = 3; }
        return rank + 1;
    }
    #region Pathfinding
    void movementUpdate()
    {
        if (inCombat) { return; }
        movementAI.validateRoute();
        if (movementAI.route != null && movementAI.route.waypoints != null && movementAI.route.waypoints.Count > 0 && wayPoint == null)
        {
            Path<Node<Tile>> altWayPoint = movementAI.followPath();
            wayPoint = altWayPoint.state.data;
            destination = new Vector2(wayPoint.position.x, wayPoint.position.y);
            Vector2 altDir = destination - position;
            if (altDir.x != 0)
                altDir.x /= Mathf.Abs(altDir.x);
            if (altDir.y != 0)
                altDir.y /= Mathf.Abs(altDir.y);
            //print("waypoint data " + destination + " " + altWayPoint.state.direction);
            if (altWayPoint.state.direction != altDir)
            {
                strafing = true;
            }
            else
            {
                rotating = true;
                moving = true;
            }
        }
        if (rotating) wheel();
        else if (moving) walk();
        else if (strafing) strafe();
        else wayPoint = null;
    }
    void wheel()
    {
        Vector2 vect = wayPoint.position - position;
        if (vect != direction)
        {
            direction = vect;
            //print("Direction " + direction);
            foreach (Model model in models)
            {
                model.setRotation(direction);
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
        rotating = false;
    }
    void walk()
    {
        foreach (Model model in models)
        {
            if (model.move())
            {
                model.moveOrder(destination);
            }
        }
        position = Vector2.MoveTowards(position, destination, type.Stats.MoveSpeed * Time.deltaTime);
        if (position.x == destination.x && position.y == destination.y)
        {
            moving = false;
        }
    }
    void strafe()
    {
        foreach (Model model in models)
        {
            if (model.move())
            {
                model.moveOrder(destination);
            }
        }
        position = Vector2.MoveTowards(position, destination, type.Stats.MoveSpeed * Time.deltaTime / 5.0f);
        if (position.x == destination.x && position.y == destination.y)
        {
            strafing = false;
        }
    }
    public float rotationCost()
    {
        return 4;
    }
    public float getMovementCost()
    {
        return 0.4f;
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