using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CaputurePoint : MonoBehaviour
{
    public delegate void Captured(CaputurePoint point, Army.Controller owner);
    public static Captured CapturedBy;
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
    int counter = 0;
    SpriteRenderer sprite;
    private void Start()
    {
        sprite = GetComponent<SpriteRenderer>();
    }
    void Update()
    {
        float gradient = Mathf.Clamp(counter, -30, 30) * Time.deltaTime / 50;
        holder = Mathf.Clamp(holder + gradient, -1, 1);
        if(holder > 0)
            sprite.color = Color.Lerp(NeutralControlled, PlayerControlled, holder);
        else
            sprite.color = Color.Lerp(NeutralControlled, AIControlled, -holder);
        if(holder > 0.8f) CapturedBy?.Invoke(this, Army.Controller.Player);
        else if (holder < -0.8)CapturedBy?.Invoke(this, Army.Controller.Computer);
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        var model = collision.GetComponent<ModelComponents.IUnitData>();
        if (model.Unit.GetComponentInParent<ArmyData>().controller == Army.Controller.Player) 
            counter++;
        else counter--;
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        var model = collision.GetComponent<ModelComponents.IUnitData>();
        if (model.Unit.GetComponentInParent<ArmyData>().controller == Army.Controller.Player)
            counter--;
        else counter++;
    }

}
