using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Master : MonoBehaviour {
    public static Master Instance;
    public Sprite grassFloor, floor2, empty;
    public HUD hud;
    public Dictionary<KeyValuePair<Unit, Unit>, CombatEngine> battles;
    Dictionary<string, ModelType> units;
    public Dictionary<Unit, Army> unitArmy;
    Army player, enemy1;
	// Use this for initialization
	void Start () {
        if(Instance != null)
        {
            return;
        }
        else
        {
            Instance = this;
        }
        initialiseUnits();
        battles = new Dictionary<KeyValuePair<Unit, Unit>, CombatEngine>();
        player = gameObject.AddComponent<Army>().create(Army.Controller.Player) as Army;
        enemy1 = gameObject.AddComponent<Army>().create(Army.Controller.Computer) as Army;
        player.addArmy(enemy1, Army.Allaince.Enemy);
        enemy1.addArmy(player, Army.Allaince.Enemy);
        unitArmy = new Dictionary<Unit, Army>();
        GameObject self = Instantiate(Resources.Load("Unit_Collider")) as GameObject;
        self.GetComponent<Unit>().create(20, 20, player, units["footman"], 6, 24);
        //unit = gameObject.AddComponent<Unit>().create(20, 20, player, units["footman"], 9, 70) as Unit;
        player.addUnit(self.GetComponent<Unit>());
        self = Instantiate(Resources.Load("Unit_Collider")) as GameObject;
        Unit u = self.GetComponent<Unit>();
        u.create(40, 20, player, units["skeleton"], 4, 16);
        Tile t = Map.Instance.getTile(40, 18);
        u.order(t);
       //unit = gameObject.AddComponent<Unit>().create(60, 80, player, units["skeleton"], 5, 37) as Unit;
       enemy1.addUnit(self.GetComponent<Unit>());
        //unit.hideFlags = HideFlags.HideInInspector;

    }
	
	// Update is called once per frame
	void Update () {
	}
    void initialiseUnits()
    {
        units = new Dictionary<string, ModelType>();
        CombatStats stats = new CombatStats
        {
            leadership = 0,
            armour = 2,
            weaponSkill = 1,
            strength = 3,
            toughness = 3,
            wounds = 1
        };
        ModelType skeleton = new ModelType
        {
            name = "skeleton",
            prefabName = "Skeleton/Skeleton",
            character = false,
            caster = false,
            unitType = UnitType.Infantry,
            movementSpeed = 4,
            combatStats = stats
        };
        units.Add(skeleton.name, skeleton);
        stats = new CombatStats
        {
            leadership = 7,
            armour = 4,
            weaponSkill = 3,
            strength = 3,
            toughness = 3,
            wounds = 1
        };
        ModelType footman = new ModelType
        {
            name = "footman",
            prefabName = "Footman/Footman",
            character = false,
            caster = false,
            unitType = UnitType.Infantry,
            movementSpeed = 12,
            combatStats = stats
        };
        units.Add(footman.name, footman);
    }
}
