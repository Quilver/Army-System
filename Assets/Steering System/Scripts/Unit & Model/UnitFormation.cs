using SoftBody;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using UnityEngine.UIElements;
using System;
using System.Reflection;

public class UnitFormation : MonoBehaviour
{
    [Range(1, 4)]
    public float ModelSize;
    [SerializeField, Range(1, 32)]
    int modelCount;
    [Range(1, 8)]
    public int Width;
    public List<Model> models;
    Rigidbody2D[] unitPins;
    UnitTemplate unit;
    // Start is called before the first frame update
    void Awake()
    {
        unit = GetComponent<UnitTemplate>();
        models = new();
        unitPins = GetComponentsInChildren<Rigidbody2D>();
        unitCollider = GetComponent<BoxCollider2D>();
        Notifications.Deployed += CreateUnit;
        transform.Find("DeploymentView").GetComponent<SpriteRenderer>().enabled = true;
        if (GetComponentInParent<Army>() == null)
            transform.Find("DeploymentView").GetComponent<SpriteRenderer>().color = Color.gray;
        else if (GetComponentInParent<Army>().controller == Army.Controller.Player)
            transform.Find("DeploymentView").GetComponent<SpriteRenderer>().color = playerColor;
        else
            transform.Find("DeploymentView").GetComponent<SpriteRenderer>().color = enemyColor;
    }
    private void OnDestroy()
    {
        Notifications.Deployed -= CreateUnit;
    }
    void CreateUnit(UnitTemplate UselessData)
    {
        GetComponent<Collider2D>().isTrigger = false;
        Destroy(transform.Find("DeploymentView"));
        for (int i = 0; i < modelCount; i++)
        {
            var model = Instantiate(this.unit.Stats.UnitPrefab);
            model.transform.position = GetModelPos(i);
            Model unitComponent = model.GetComponent<Model>();
            unitComponent.Setup(unitPins, transform);
            models.Add(unitComponent);
        }
        DrawGizmo = false;
        GetComponent<UnitTemplate>().unitState = UnitState.Idle;
        GameObject formationImage = transform.Find("DeploymentView").gameObject;
        DestroyImmediate(formationImage);
    }
    BoxCollider2D unitCollider;
    private void Update()
    {
        unitCollider.offset = unitOffset - (Vector2)transform.position;
        unitCollider.size = UnitSize;
        if(unit.unitState == UnitState.Deployment)
        {
            transform.Find("DeploymentView").transform.localPosition = unitCollider.offset;
            transform.Find("DeploymentView").transform.localScale = unitCollider.size;
        }
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
        

        int y = (modelIndex / Width);
        Vector3 offset = new Vector3(x, -y) * ModelSize / 2;
        offset = Quaternion.AngleAxis(transform.eulerAngles.z, Vector3.forward) * offset;
        return transform.position + offset;
    }
    public void Death(Model model)
    {
        if(!models.Contains(model)) return;
        int position = models.FindIndex(a=> a== model);
        Reposition(position);
        model.GetComponent<ModelDeath>().Setup();
        GetComponent<UnitTemplate>().TakeDamage(models.Count);
    }
    public void DestroyUnit()
    {
        foreach (var model in models)
            model.GetComponent<ModelDeath>().Setup();
    }
    void Reposition(int ModelToReplaceIndex)
    {
        //shift up
        if (ModelToReplaceIndex + Width < models.Count)
        {
            models[ModelToReplaceIndex] = models[ModelToReplaceIndex + Width];
            models[ModelToReplaceIndex].ResetPositionInFormation(unitPins, GetModelPos(ModelToReplaceIndex));
            Reposition(ModelToReplaceIndex+Width);
        }
        //shift right
        else if(ModelToReplaceIndex+2 < models.Count)
        {
            models[ModelToReplaceIndex] = models[ModelToReplaceIndex + 2];
            models[ModelToReplaceIndex].ResetPositionInFormation(unitPins, GetModelPos(ModelToReplaceIndex));
            Reposition(ModelToReplaceIndex+2);
        }
        //done
        else models.RemoveAt(ModelToReplaceIndex);
    }
    [SerializeField]
    bool DrawGizmo;
    Color unalignedColor = Color.gray;
    Color playerColor = new Color(0, 0, 1, 0.7f);
    Color enemyColor = new Color(1, 0, 0, 0.7f);
    private void OnDrawGizmos()
    {
        if (!DrawGizmo) return;
        if(GetComponentInParent<Army>() == null)
            Gizmos.color = Color.gray;
        else if (GetComponentInParent<Army>().controller == Army.Controller.Player)
            Gizmos.color = playerColor;
        else
            Gizmos.color = enemyColor;
        for (int i = 0; i < modelCount; i++) Gizmos.DrawSphere(GetModelPos(i), ModelSize / 5);
        
    }
}
