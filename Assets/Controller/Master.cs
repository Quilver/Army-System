using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Master : MonoBehaviour {
    public static Master Instance;
    public Dictionary<KeyValuePair<Unit, Unit>, CombatEngine> battles;
    public Dictionary<Unit, Army> unitArmy;
    [SerializeField] Army player, enemy1;
	// Use this for initialization
	void Awake () {
        if(Instance != null)
        {
            return;
        }
        else
        {
            Instance = this;
        }
        battles = new Dictionary<KeyValuePair<Unit, Unit>, CombatEngine>();
        unitArmy = new Dictionary<Unit, Army>();
        //GameObject self = Instantiate(Resources.Load("Unit_Collider")) as GameObject;
        //self = Instantiate(Resources.Load("Unit_Collider")) as GameObject;
        //Tile t = Map.Instance.getTile(40, 18);
        
    }
	
	// Update is called once per frame
	void Update () {
	}
}
