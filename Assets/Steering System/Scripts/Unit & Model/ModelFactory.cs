using SoftBody;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using UnityEngine.UIElements;

public class ModelFactory : MonoBehaviour
{
    [SerializeField]
    GameObject ModelPrefab;
    [Range(1, 4)]
    public float ModelSize;
    [SerializeField, Range(1, 32)]
    int modelCount;
    [Range(1, 8)]
    public int Width;
    public List<Model> models;
    // Start is called before the first frame update
    void Awake()
    {
        models = new();
        for (int i = 0; i < modelCount; i++)
        {
            var model = Instantiate(ModelPrefab);
            model.transform.position = GetModelPos(i);
            Model unitComponent = model.GetComponent<Model>();
            unitComponent.Setup(GetComponentsInChildren<Rigidbody2D>(), transform);
            models.Add(unitComponent);
        }
        unitCollider = GetComponent<BoxCollider2D>();
        DrawGizmo =false;
    }
    BoxCollider2D unitCollider;
    private void Update()
    {
        unitCollider.offset = unitOffset - (Vector2)transform.position;
        unitCollider.size = UnitSize;
    }
    Vector2 UnitSize
    {
        get
        {
            return new Vector2(Width, Ranks) * ModelSize / 2;
        }
    }
    public int Files
    {
        get
        {
            if (models == null || models.Count >= Width || models.Count == 0) return Width;
            return models.Count;
        }
    }
    public int Ranks
    {
        get
        {
            return (modelCount % Files > 0) ? modelCount / Files + 1 : modelCount / Files;
        }
    }
    Vector2 unitOffset
    {
        get
        {
            return new Vector2(transform.position.x, transform.position.y - (Ranks - 1) * ModelSize / 4);
        }
    }
    Vector2 GetModelPos(int modelIndex)
    {
        float x = modelIndex % Width;
        if (x % 2 == 0) x = -x / 2;
        else x = x / 2 + 0.5f;
        if (Width % 2 == 0) x -= 0.5f;
        //x -= (Width%2 ==0) ? Width/2 - 0.5f : Width/2;

        int y = (modelIndex / Width);
        Vector3 offset = new Vector3(x, -y) * ModelSize / 2;
        offset = Quaternion.AngleAxis(transform.eulerAngles.z, Vector3.forward) * offset;
        return transform.position + offset;
    }
    [SerializeField]
    bool DrawGizmo;
    private void OnDrawGizmos()
    {
        if (!DrawGizmo) return;
        if (GetComponentInParent<Army>().controller == Army.Controller.Player)
            Gizmos.color = new Color(0, 0, 1, 0.7f);
        else
            Gizmos.color = new Color(1, 0, 0, 0.7f);
        for (int i = 0; i < modelCount; i++) Gizmos.DrawSphere(GetModelPos(i), ModelSize / 5);
        DrawArcs();
        DrawArc();
        TestAngleOfPoint();
        
    }
    private void DrawArcs()
    {
        Vector3 Center = (Vector2)transform.position + GetComponent<Collider2D>().offset;

        Vector3 LF, RF, LB, RB;
        if (modelCount <= 1) { LF = Center; RF = LF; LB = Center; RB = Center; }
        else { 
            LF = GetModelPos(Width - 2); LB = GetModelPos(- 2 + Ranks * Width); 
            RF = GetModelPos(Width - 1); RB = GetModelPos(- 1 + Ranks * Width);
        }
        if (Width % 2 == 1)
        {
            var temp = LB; 
            LB = RB; RB = temp;
            temp = LF; LF = RF; RF=temp;    
        }
        Vector2 forward = transform.up;
        Gizmos.color = Color.green;
        Vector2 leftLine = Quaternion.AngleAxis(45f, Vector3.forward) * forward * 3 + LF;
        Vector2 rightLine = Quaternion.AngleAxis(-45f, Vector3.forward) * forward * 3 + RF;
        Gizmos.DrawLine(RF, rightLine);
        Gizmos.DrawLine(leftLine, LF);
        Gizmos.DrawLine(leftLine, rightLine);
        
        Gizmos.color= Color.red;
        var leftLineB = Quaternion.AngleAxis(135f, Vector3.forward) * forward * 3 + LB;
        var rightLineB = Quaternion.AngleAxis(-135f, Vector3.forward) * forward * 3 + RB;
        Gizmos.DrawLine(RB, rightLineB);
        Gizmos.DrawLine(leftLineB, LB);
        Gizmos.DrawLine(leftLineB, rightLineB);
        
        Gizmos.color=Color.yellow;
        Gizmos.DrawLine(leftLine, leftLineB);
        Gizmos.DrawLine(rightLine, rightLineB);
    }
    void DrawArc()
    {
        var Center = transform.position + (Vector3)GetModelPos((Ranks-1) * Width);Center /= 2;
        var angle = Vector2.SignedAngle(transform.up, GetModelPos(Width-1) - (Vector2)Center);
        Gizmos.color = Color.black;
        Gizmos.DrawLine(Center, Quaternion.AngleAxis(angle, Vector3.forward) * transform.up * 3 +Center);
        Gizmos.DrawLine(Center, Quaternion.AngleAxis(angle*2, Vector3.forward) * transform.up * 3 + Center);
    }
    [SerializeField] Transform TestPoint;
    [SerializeField] float Angle;
    void TestAngleOfPoint()
    {
        if(TestPoint==null)return;
        Angle = Vector2.SignedAngle(transform.up, TestPoint.position- transform.position);
        SpriteRenderer sprite = TestPoint.GetComponent<SpriteRenderer>();
        if (Mathf.Abs(Angle) < 45) sprite.color = Color.green;
        else if (Mathf.Abs(Angle) < 135) sprite.color = Color.yellow;
        else sprite.color = Color.red;

    }
}
