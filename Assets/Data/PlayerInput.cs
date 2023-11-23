using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInput : MonoBehaviour {
    public static PlayerInput Instance { get; protected set; }
    public Section selectedObject, hoverObject;
    public List<Unit> selectedUnits;
    Map map;
    float targetOrtho;
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
    void moveCamera()
    {
        //move camera
        float xAxisValue = Input.GetAxis("Horizontal");
        float yAxisValue = Input.GetAxis("Vertical");
        float xDir = (xAxisValue + yAxisValue) * 0.3f;
        float yDir = (yAxisValue - xAxisValue) * 0.3f;
        if (Camera.main != null)
        {
            Camera.main.transform.position = new Vector3(Camera.main.transform.position.x + xDir, Camera.main.transform.position.y + yDir, Camera.main.transform.position.z);
            //Camera.main.transform.Translate(new Vector3(xAxisValue*0.6f, yAxisValue*0.6f, 0));
        }
        //zoom camera
        float zoomSpeed = 5;
        float smoothSpeed = 10f;// 2.0f;
        float minOrtho = 1.0f;
        float maxOrtho = 30.0f;
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
            if (selectedObject != null && selectedObject.unit != null)
            {
                selectedUnits.Add(selectedObject.unit);
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
        //first we get the ray represented by the mouse click. because its a perspective camera, 
        //the direction will change depending on where you click
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        //the ray origin is the position in world space on the 'front' plane of the camera. however
        //for your game we need to go forwards to the plane of your objects. I am going to assume
        //that is z==0 for now, so...
        float z_plane_of_2d_game = 0;
        Vector3 pos_at_z_0 = ray.origin + ray.direction * (z_plane_of_2d_game - ray.origin.z) / ray.direction.z;

        //now we have a 3D point on the correct plane, we can continue with your code
        Vector2 point = new Vector2(pos_at_z_0.x, pos_at_z_0.y);
        RaycastHit2D hit = Physics2D.Raycast(point, Vector2.zero, Mathf.Infinity, 1 << LayerMask.NameToLayer("Terrain") );
        //RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
        if (hit.collider != null)
        {
            GameObject obj = hit.collider.gameObject;
            if (Map.Instance.get_tile.ContainsKey(obj))
            {
                hoverObject = Map.Instance.get_tile[obj].select() as Section;
            }
        }
        else
        {
            hoverObject = null;
        }
    }
    bool inBounds()
    {
        //Screen coordinates start in the lower-left corner of the screen
        //not the top-left of the screen like the drawing coordinates do
        Vector3 mousePos = Input.mousePosition;
        bool insideWidth = mousePos.x >= 0 && mousePos.x <= Screen.width - HUD.UNIT_BAR_WIDTH;
        bool insideHeight = mousePos.y >= 0 && mousePos.y <= Screen.height - HUD.TILE_BAR_HEIGHT;
        bool inScreen = insideWidth && insideHeight;
        
        return inScreen;
    }
    bool inBounds(int x, int y)
    {
        //Screen coordinates start in the lower-left corner of the screen
        //not the top-left of the screen like the drawing coordinates do
        bool insideWidth = x >= 0 && x <= Screen.width - HUD.UNIT_BAR_WIDTH;
        bool insideHeight = y >= 0 && y <= Screen.height - HUD.TILE_BAR_HEIGHT;
        bool inScreen = insideWidth && insideHeight && (x >= 0 && x < Map.Instance.width) && (y >= 0 && y < Map.Instance.hieght);

        return inScreen;
    }
    void giveOrder()
    {
        //print("Giving order");
        foreach (Unit unit in selectedUnits)
        {
            if(Master.Instance.unitArmy[unit].controller == Army.Controller.Player)
            {
                unit.order(hoverObject);
            }
        }
    }
}
