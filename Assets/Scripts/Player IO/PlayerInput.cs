using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerInput : MonoBehaviour {
    [SerializeField] Army playerArmy;
    public static PlayerInput Instance { get; protected set; }
    //public SelectionData selectedObject, hoverObject;
    public UnitR selectedUnit, hoverUnit;
    [SerializeField]
    Vector2 MinimumCameraBounds, MaxCameraBounds;
    float targetOrtho;
    [SerializeField] Transform cursor;
    public Vector2Int Cursor
    {
        get
        {
            return new((int)cursor.position.x, (int)cursor.position.y);
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
        targetOrtho = Camera.main.orthographicSize;
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = new(255, 0, 0, 50);
        Vector3 center = (MinimumCameraBounds + MaxCameraBounds)/2;
        Vector3 size = MaxCameraBounds - MinimumCameraBounds;
        size.x = Mathf.Abs(size.x);
        size.y = Mathf.Abs(size.y);
        Gizmos.DrawWireCube(center, size);
    }
    // Update is called once per frame
    void Update () {
        GetMouseHover();
        SelectItem();
        MoveCamera();
        if (Input.GetKeyDown(KeyCode.Escape))
            SceneManager.LoadScene(0);
    }
    [SerializeField]
    float cameraSpeed = 5;
    [SerializeField]
    float zoomSpeed = 3;
    void MoveCamera()
    {
        //move camera
        float xAxisValue = Input.GetAxis("Horizontal");
        float yAxisValue = Input.GetAxis("Vertical");
        float xDir = (xAxisValue) * cameraSpeed * Time.deltaTime;
        float yDir = (yAxisValue) * cameraSpeed * Time.deltaTime;
        var camera = Camera.main;
        if (camera != null)
        {
            camera.transform.position = new Vector3(camera.transform.position.x + xDir, camera.transform.position.y + yDir, camera.transform.position.z);
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
        ClampCamera();
        camera.orthographicSize = Mathf.MoveTowards(camera.orthographicSize, targetOrtho, smoothSpeed * Time.deltaTime);
    }
    void ClampCamera()
    {
        var camera = Camera.main;
        float screenHeightInUnits = camera.orthographicSize;// * 2;
        float screenWidthInUnits = screenHeightInUnits * Screen.width / Screen.height;
        Vector3 position= camera.transform.position;
        if(position.x < MinimumCameraBounds.x+screenWidthInUnits)
            position.x = MinimumCameraBounds.x + screenWidthInUnits;
        else if (position.x > MaxCameraBounds.x - screenWidthInUnits)
            position.x = MaxCameraBounds.x - screenWidthInUnits;
        if (position.y < MinimumCameraBounds.y + screenHeightInUnits)
            position.y = MinimumCameraBounds.y+ screenHeightInUnits;
        else if (position.y > MaxCameraBounds.y - screenHeightInUnits)
            position.y = MaxCameraBounds.y - screenHeightInUnits;
        camera.transform.position = position;   
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
        cursor.position = worldPosition;
        var coll = Physics2D.OverlapCircle(worldPosition, 0.6f, 1 << 6);
        if (coll != null)
            hoverUnit = coll.GetComponentInParent<UnitR>();
    }
    void GiveOrder()
    {
        if(selectedUnit!= null && Battle.Instance.unitArmy[selectedUnit].controller == Army.Controller.Player) { 
            selectedUnit.Movement.MoveTo(Cursor); 
        }
    }
}
