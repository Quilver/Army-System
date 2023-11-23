using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum UnitType
{
    Infantry, Cavalry, Monsters, Collosuss
}
public class Unit:MonoBehaviour{
    //unit details
    Army army;
    public List<Model> models;
    public ModelType typeOfModel;
    int width;
    int numberOfModels = 24;
    //movement
    Vector2 destination;
    public Vector2 direction, position;
    Vector3 targetRotation;
    public float moveSpeed, rotateSpeed;
    public bool rotating, moving, strafing;
    Tile wayPoint;
    UnitMovementAI movementAI;
    //combat
    public bool inCombat, routing;
    float leaderShipPenalty, combatLeadershipPenalty, deathsPenalty;
    //
    public Unit create(int x, int y, Army army, ModelType modelType, int width, int models)
    {
        gameObject.name = modelType.name;
        this.army = army;
        //movement
        position = new Vector2(x, y);
        Map.Instance.getTile(x, y).unit = this;
        rotating = false;
        moving = false;
        strafing = false;
        moveSpeed = modelType.movementSpeed;
        //model initialisation
        this.width = width;
        numberOfModels = models;
        typeOfModel = modelType;
        populateModels();
        updateCollider();
        //movement ai
        movementAI = new UnitMovementAI(this);
        return this;
    }
    void populateModels()
    {
        //populate unit with models
        models = new List<Model>();
        int count = numberOfModels;
        if (width > numberOfModels) { width = numberOfModels; }
        int xOffset = width / 2;
        int yOffset = (int)Mathf.Ceil((numberOfModels * 1.0f) / width);
        //Add models to unit
        for (int y = 0; y < yOffset; y++)
        {
            for  (int x = -xOffset; x < width - xOffset; x++)
            {
                if (count <= 0) { break; }
                Vector2 offset = new Vector2(x, y);
                models.Add(gameObject.AddComponent<Model>().create((int)position.x, (int)position.y, offset, this, typeOfModel.prefabName) as Model);
                models[models.Count - 1].hideFlags = HideFlags.HideInInspector;
                count--;
            }
        }
    }
    public string getDetails()
    {
        string details = typeOfModel.name + " at (x " + (int)position.x + ", y" + (int)position.y + ")";
        details += "\nNumber of models " + numberOfModels;
        details += "\nMovement speed " + typeOfModel.movementSpeed;
        details += "\nLeadership " + typeOfModel.combatStats.leadership;
        details += "\nHit points " + typeOfModel.combatStats.wounds;
        details += "\nEndurance " + typeOfModel.combatStats.toughness;
        details += "\nArmour " + typeOfModel.combatStats.armour;
        details += "\nMelee skill " + typeOfModel.combatStats.weaponSkill;
        details += "\nStrength " + typeOfModel.combatStats.strength;
        return details;
    } 

    // Update is called once per frame
    public void Update () {
        movementUpdate();
        updateCollider();
        updateLeadership();
    }
    public void order(Section section)
    {
        //Debug.Log("Recieved order");
        if (section == null) { return; }
        else if(section.unit != null)// && army.enemies.Contains(section.unit.army))
        {
            movementAI.getRoute(section.unit as Unit);
        }
        else if (section.GetType() == typeof(Tile))
        {
            movementAI.getRoute(section as Tile);
        }
        
    }
    //combat
    public void updateCollider()
    {
        numberOfModels = models.Count;
        if(numberOfModels < width)
        {
            width = numberOfModels;
        }
        BoxCollider2D box = gameObject.GetComponent<BoxCollider2D>();
        box.size = new Vector2(width, Mathf.Ceil(numberOfModels / (width + 0f)));
        box.offset = new Vector2(0, Mathf.Ceil(numberOfModels / (width * 2f)) - 0.5f);
        gameObject.transform.position = position;
        gameObject.transform.rotation = Quaternion.AngleAxis(CombatEngine.AngleBetweenVector2(position, models[0].position) - 180, Vector3.forward);
    }
    public void deaths(int numberOfDeaths) {
        if(models==null || models.Count == 0) { return; }
        if(models.Count <= numberOfDeaths)
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
    void OnCollisionEnter2D(Collision2D col) {
       if(col.gameObject.tag == "Regiment" && col.gameObject.GetComponent<Unit>() != null)
        {
            //print("Collided with unit");
            Unit u = col.gameObject.GetComponent<Unit>() as Unit;
            KeyValuePair<Unit, Unit> pa = new KeyValuePair<Unit, Unit>(this, u);
            //print("Collision between two units");
            //print(getDetails());
            //print(u.getDetails());
            if (!Master.Instance.battles.ContainsKey(new KeyValuePair<Unit, Unit>(u, this)) && !Master.Instance.battles.ContainsKey(pa))// && army.enemies.Contains(u.army))
            {
                //print("Entered combat");
                inCombat = true;
                Master.Instance.battles.Add(pa, gameObject.AddComponent<CombatEngine>().create(this, u) as CombatEngine);
            }
        }
    }
    void updateLeadership()
    {

    }
    public bool adjacentTile(Vector2 position)
    {
        bool adjacent = false;
        foreach(Model model in models)
        {
            if(1 >= Mathf.Abs(model.position.x - position.x) && 1 >= Mathf.Abs(model.position.y - position.y))
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
        foreach(Model model in models)
        {
            if( obj.GetComponent<BoxCollider2D>().IsTouching(model.self.GetComponent<BoxCollider2D>()))
            {
                contacts++;
                Vector2 pos = (Vector2)model.self.transform.position;
                pos.x = Mathf.Round(pos.x);
                pos.y = Mathf.Round(pos.y);
                if (!setAngle)
                {
                    string names = "";
                    names += (pos + direction) + " ";
                    if(Map.Instance.getTile(pos + direction).unit != null) {
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
                if (setAngle) {
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
                    fight = CombatEngine.fight.Flank ;
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
        int rank = numberOfModels / width;
        if(rank > 3) { rank = 3; }
        return rank + 1;
    }
    //pathfinding
    void movementUpdate()
    {
        if (inCombat) { return; }
        movementAI.validateRoute();
        if(movementAI.route != null && movementAI.route.waypoints !=null && movementAI.route.waypoints.Count > 0 && wayPoint == null)
        {
            Path<Node<Tile>> altWayPoint = movementAI.followPath();
            wayPoint = altWayPoint.state.data;
            destination = new Vector2(wayPoint.position.x, wayPoint.position.y);
            Vector2 altDir = destination - position;
            if(altDir.x != 0)
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
        else {  }
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
        position = Vector2.MoveTowards(position, destination, moveSpeed * Time.deltaTime);
        if(position.x ==destination.x && position.y == destination.y)
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
        position = Vector2.MoveTowards(position, destination, moveSpeed * Time.deltaTime / 5.0f);
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
}
