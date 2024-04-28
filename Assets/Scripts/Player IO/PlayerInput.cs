using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerInput : MonoBehaviour {
    [SerializeField] Army playerArmy;
    public static PlayerInput Instance { get; protected set; }
    //public SelectionData selectedObject, hoverObject;
    public UnitR selectedUnit, hoverUnit;
    [SerializeField] Transform mouseHighlight;
    public Vector2Int MouseHighlight
    {
        get
        {
            int x = Mathf.RoundToInt(mouseHighlight.position.x);
            int y = Mathf.RoundToInt(mouseHighlight.position.y);
            return new(x, y);
        }
    }
    // Use this for initialization
    void Start() {
        if (Instance != null)
        {
            Debug.Log("Error: there should only be one player input instance");
        }
        else
        {
            Instance = this;
        }
        Cursor.visible = false;
        
    }
    // Update is called once per frame
    void Update () {
        GetMouseHover();
        SelectItem();
        if (Input.GetKeyDown(KeyCode.Escape))
            SceneManager.LoadScene(0);
    }
    void SelectItem()
    {
        if (Input.GetMouseButtonDown(0))
        {
            selectedUnit = hoverUnit;
        }
        else if (Input.GetMouseButtonDown(1))
        {
            GiveOrder();
        }
    }
    void GetMouseHover()
    {
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        worldPosition.x = (int)worldPosition.x;
        worldPosition.y = (int)worldPosition.y;
        worldPosition.z = 0;
        mouseHighlight.position = worldPosition;
        var coll = Physics2D.OverlapCircle(worldPosition, 0.6f, 1 << 6);
        if (coll != null)
            hoverUnit = coll.GetComponentInParent<UnitR>();
        else hoverUnit = null;
        if (hoverUnit == null) mouseHighlight.GetComponent<SpriteRenderer>().color = Color.white;
        else if (Battle.Instance.unitArmy[hoverUnit].controller == Army.Controller.Player)
            mouseHighlight.GetComponent<SpriteRenderer>().color = Color.blue;
        else mouseHighlight.GetComponent<SpriteRenderer>().color = Color.red;
    }
    void GiveOrder()
    {
        if(selectedUnit!= null && Battle.Instance.unitArmy[selectedUnit].controller == Army.Controller.Player) { 
            selectedUnit.Movement.MoveTo(MouseHighlight); 
        }
    }
}
