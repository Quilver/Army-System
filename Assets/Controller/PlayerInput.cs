using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInput : MonoBehaviour {
    [SerializeField] Army playerArmy;
    public static PlayerInput Instance { get; protected set; }
    public SelectionData selectedObject, hoverObject;
    public List<Unit> selectedUnits;
    Map map;
    float targetOrtho;
    [SerializeField] Transform cursor;
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
        selectedObject = null;
        targetOrtho = Camera.main.orthographicSize;
        //startPosition = new Vector3();
        //currentPosition = new Vector3();
    }
	
	// Update is called once per frame
	void Update () {
        getMouseHover();
        selectItem();
        moveCamera();
    }
    [SerializeField]
    float cameraSpeed = 5;
    [SerializeField]
    float zoomSpeed = 3;
    void moveCamera()
    {
        //move camera
        float xAxisValue = Input.GetAxis("Horizontal");
        float yAxisValue = Input.GetAxis("Vertical");
        float xDir = (xAxisValue) * cameraSpeed * Time.deltaTime;
        float yDir = (yAxisValue) * cameraSpeed * Time.deltaTime;
        if (Camera.main != null)
        {
            Camera.main.transform.position = new Vector3(Camera.main.transform.position.x + xDir, Camera.main.transform.position.y + yDir, Camera.main.transform.position.z);
            //Camera.main.transform.Translate(new Vector3(xAxisValue*0.6f, yAxisValue*0.6f, 0));
        }
        //zoom camera
        float smoothSpeed = 10f;// 2.0f;
        float minOrtho = 3.0f;
        float maxOrtho = 10.0f;
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0.0f)
        {
            targetOrtho -= scroll * zoomSpeed;
            targetOrtho = Mathf.Clamp(targetOrtho, minOrtho, maxOrtho);
        }

        Camera.main.orthographicSize = Mathf.MoveTowards(Camera.main.orthographicSize, targetOrtho, smoothSpeed * Time.deltaTime);
    }
    void selectItem()
    {
        if (Input.GetMouseButtonDown(0))
        {
            //print(hoverObject);
            selectedObject = hoverObject;
            selectedUnits.Clear();
            if(selectedObject != null && selectedObject is Tile)
                if((selectedObject as Tile).unit!= null)
                    selectedUnits.Add((selectedObject as Tile).unit);
            if (selectedObject != null && (selectedObject as Unit) != null)
            {
                selectedUnits.Add((selectedObject as Unit));
            }
        }
        else if (Input.GetMouseButtonDown(1))
        {
            //print("Right click to order");
            giveOrder();
        }
    }
    void getMouseHover()
    {
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        worldPosition.x = (int)worldPosition.x;
        worldPosition.y = (int)worldPosition.y;
        worldPosition.z = 0;
        cursor.position = worldPosition;
        hoverObject= Map.Instance.getTile(worldPosition);
    }
    void giveOrder()
    {
        //print("Giving order");
        foreach (Unit unit in selectedUnits)
        {
            if(unit.Controller == playerArmy)
            {
                unit.order(hoverObject);
            }
        }
    }
}
