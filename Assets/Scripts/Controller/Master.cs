using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Master : MonoBehaviour {
    public static Master Instance;
    public Combats combats;
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
        combats = new Combats();
        unitArmy = new Dictionary<Unit, Army>();
        
    }
	
	// Update is called once per frame
	void Update () {
	}
}
