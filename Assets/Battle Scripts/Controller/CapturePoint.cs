using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CapturePoint : MonoBehaviour
{
    public enum PointController
    {
        Neutral,
        Player,
        Enemy
    }
    public PointController Controller
    {
        get
        {
            if(holder > triggerThreshold) return PointController.Player;
            else if(holder < triggerThreshold) return PointController.Enemy;
            else return PointController.Neutral;
        }
    }
    public static System.Action<CapturePoint, Army> CapturedBy;
    public enum CapturePointType
    {
        Fortress,
        Village,
        Banner,
        Misc
    }
    [SerializeField]
    Color PlayerControlled, NeutralControlled, AIControlled;
    [SerializeField, Range(-1, 1)]
    float holder;
    [SerializeField, Range(0, 1)]
    float triggerThreshold = 0.8f;
    public event System.Action<bool> capturedBy;
    public static event System.Action<CapturePoint, bool> PointCaptured;
    void InvokeCapture(bool playerCaptured)
    {
        capturedBy?.Invoke(playerCaptured);
        PointCaptured?.Invoke(this, playerCaptured);
    }
    int counter = 0;
    SpriteRenderer sprite;
    private void Start()
    {
        sprite = GetComponent<SpriteRenderer>();
    }
    void Update()
    {
        bool belowThreshold = Mathf.Abs(holder) > triggerThreshold;

        CalculateHolder();
        UpdateColour();
        if (belowThreshold && Mathf.Abs(holder) > triggerThreshold)
            InvokeCapture(holder > 0);
    }
    void CalculateHolder()
    {
        float gradient = Mathf.Clamp(counter, -30, 30) * Time.deltaTime / 50;
        holder = Mathf.Clamp(holder + gradient, -1, 1);
    }
    void UpdateColour()
    {
        if (holder > 0)
            sprite.color = Color.Lerp(NeutralControlled, PlayerControlled, holder);
        else
            sprite.color = Color.Lerp(NeutralControlled, AIControlled, -holder);
        if (holder > 0.8f) CapturedBy?.Invoke(this, Battle.Instance.player);
        else if (holder < -0.8) CapturedBy?.Invoke(this, Battle.Instance.enemy);
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        var model = collision.GetComponent<ModelComponents.IUnitData>();
        if (model.Unit.GetComponentInParent<Army>() == Battle.Instance.player) 
            counter++;
        else counter--;
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        var model = collision.GetComponent<ModelComponents.IUnitData>();
        if(model == null || model.Unit == null || model.Unit.GetComponentInParent<Army>()) return;
        if (model.Unit.GetComponentInParent<Army>() == Battle.Instance.player)
            counter--;
        else counter++;
    }

}
