using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
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
    Player inputs;
    // Use this for initialization
    void Awake() {
        if (Instance != null)
        {
            Debug.Log("Error: there should only be one player input instance");
        }
        else
        {
            Instance = this;
        }
        Cursor.visible = false;
        inputs = new Player();
    }
    #region Player inputs
    private void OnEnable()
    {
        inputs.Enable();
        inputs.CursorControls.MoveCursor.performed += MoveCursor;
        inputs.CursorControls.MoveCursor.canceled += MoveCursor;
        inputs.CursorControls.SetCursor.performed += SetCursor;
        inputs.CursorControls.Select.performed += Select;
        inputs.CursorControls.Order.performed += Order;
        inputs.CursorControls.Pause.performed += Pause;
        inputs.CursorControls.ToggleUnits.performed += ToggleUnits;


    }
    private void OnDisable()
    {
        inputs.Disable();
        inputs.CursorControls.MoveCursor.performed -= MoveCursor;
        inputs.CursorControls.MoveCursor.canceled -= MoveCursor;
        inputs.CursorControls.SetCursor.performed -= SetCursor;
        inputs.CursorControls.Select.performed -= Select;
        inputs.CursorControls.Order.performed -= Order;
        inputs.CursorControls.Pause.performed -= Pause;
        inputs.CursorControls.ToggleUnits.performed -= ToggleUnits;
    }
    Vector2 cursorDirection= Vector2.zero;
    [SerializeField, Range(15, 35)]
    float cursorSpeed;
    Vector2 CursorPositionOffset;
    void MoveCursor(InputAction.CallbackContext value)
    {
        cursorDirection = value.ReadValue<Vector2>().normalized * cursorSpeed;
    }
    void SetCursor(InputAction.CallbackContext value) { 
        CursorPositionOffset = Camera.main.ScreenToWorldPoint(Input.mousePosition) - Camera.main.transform.position;

    }
    void Select(InputAction.CallbackContext value)
    {
        selectedUnit = hoverUnit;
    }
    void Order(InputAction.CallbackContext value)
    {
        GiveOrder();
    }
    void Pause(InputAction.CallbackContext value)
    {
        if (Time.timeScale == 1) Time.timeScale = 0;
        else Time.timeScale = 1;
    }
    int _selectedUnitIndex = 0;
    void ToggleUnits(InputAction.CallbackContext value)
    {
        _selectedUnitIndex += Mathf.RoundToInt(value.ReadValue<float>());
        if(_selectedUnitIndex < 0) _selectedUnitIndex+= playerArmy.units.Count;
        _selectedUnitIndex%= playerArmy.units.Count;
        selectedUnit = playerArmy.units[_selectedUnitIndex];
        CursorPositionOffset = Vector2.zero;
        var pos = selectedUnit.Movement.position.Location;
        Camera.main.transform.position = new(pos.x, pos.y, -5);

    }
    #endregion


    void Update () {
        UpdateCursorPosition();
        //GetMouseHover();
        //SelectItem();
        if (Input.GetKeyDown(KeyCode.Escape))
            SceneManager.LoadScene(0);
    }
    void UpdateCursorPosition()
    {
        CursorPositionOffset += cursorDirection * Time.unscaledDeltaTime;
        transform.position = ClampCursorPositionInWorld();
        UpdateHover();
    }
    Vector3 ClampCursorPositionInWorld()
    {
        if (CursorPositionOffset.x < -Camera.main.aspect * Camera.main.orthographicSize)
            CursorPositionOffset.x = -Camera.main.aspect * Camera.main.orthographicSize;
        else if (CursorPositionOffset.x > Camera.main.aspect * Camera.main.orthographicSize)
            CursorPositionOffset.x = Camera.main.aspect * Camera.main.orthographicSize;
        float x = Mathf.Round(Camera.main.transform.position.x + CursorPositionOffset.x);
        if (CursorPositionOffset.y < -Camera.main.orthographicSize)
            CursorPositionOffset.y = -Camera.main.orthographicSize;
        else if (CursorPositionOffset.y > Camera.main.orthographicSize)
            CursorPositionOffset.y = Camera.main.orthographicSize;
        float y = Mathf.Round(Camera.main.transform.position.y + CursorPositionOffset.y);
        return new(x, y, 0);
    }
    void UpdateHover()
    {
        var coll = Physics2D.OverlapCircle(transform.position, 0.6f, 1 << 6);
        if (coll != null)
            hoverUnit = coll.GetComponentInParent<UnitR>();
        else hoverUnit = null;
        if (hoverUnit == null) mouseHighlight.GetComponent<SpriteRenderer>().color = Color.white;
        else if (Battle.Instance.unitArmy[hoverUnit].controller == Army.Controller.Player)
            mouseHighlight.GetComponent<SpriteRenderer>().color = Color.blue;
        else mouseHighlight.GetComponent<SpriteRenderer>().color = Color.red;
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
