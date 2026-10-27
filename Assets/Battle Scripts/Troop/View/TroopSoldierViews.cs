using System.Collections.Generic;
using UnityEngine;

// One sprite GameObject (with a kinematic body and collider) per soldier, kept in step with the soldier simulation.
// Renderer i always belongs to soldier i of the roster.
public sealed class TroopSoldierViews
{
    readonly Troop _troop;
    readonly List<SpriteRenderer> _renderers = new();
    readonly ClashPulseView _pulse = new();

    public TroopSoldierViews(Troop troop)
    {
        _troop = troop;
    }

    public void Create()
    {
        DestroyAll();
        foreach (var soldier in _troop.Roster.SoldierList)
            _renderers.Add(CreateSoldierObject(soldier));
        Refresh(0f);
    }

    public void Refresh(float deltaTime)
    {
        List<SoldierAgentState> soldiers = _troop.Roster.SoldierList;
        int count = Mathf.Min(soldiers.Count, _renderers.Count);
        Color armyColor = _troop.ArmyColor;
        for (int i = 0; i < count; i++)
        {
            var soldier = soldiers[i];
            var renderer = _renderers[i];
            if (renderer == null) continue;

            renderer.enabled = soldier.IsAlive;
            renderer.color = armyColor;
            UpdatePulse(renderer, soldier, deltaTime);
            if (!soldier.IsAlive) continue;
            renderer.transform.position = _troop.transform.TransformPoint(soldier.Position);
            renderer.transform.rotation = Quaternion.Euler(0f, 0f, SoldierFacingAnimator.Update(_troop, soldier, deltaTime));
        }
    }

    public void PulseClash(SoldierAgentState soldier, float duration)
    {
        int index = _troop.Roster.SoldierList.IndexOf(soldier);
        if (index < 0 || index >= _renderers.Count) return;
        SpriteRenderer renderer = _renderers[index];
        if (renderer == null) return;

        soldier.BeginClashColorPulse(duration);
        _pulse.Apply(renderer, soldier.ClashColorPulseProgress);
    }

    public void SetVisible(bool visible)
    {
        foreach (var renderer in _renderers)
            if (renderer != null) renderer.enabled = visible;
    }

    public void DestroyAll()
    {
        foreach (var renderer in _renderers)
            if (renderer != null) TroopObjects.Destroy(renderer.gameObject);
        _renderers.Clear();
    }

    void UpdatePulse(SpriteRenderer renderer, SoldierAgentState soldier, float deltaTime)
    {
        bool pulseEnded = soldier.TickClashColorPulse(deltaTime);
        if (soldier.IsClashColorPulsing)
            _pulse.Apply(renderer, soldier.ClashColorPulseProgress);
        else if (pulseEnded)
            _pulse.Apply(renderer, 1f);
    }

    SpriteRenderer CreateSoldierObject(SoldierAgentState soldier)
    {
        var visual = new GameObject($"Soldier {soldier.Id}");
        int unitLayer = LayerMask.NameToLayer("Unit");
        if (unitLayer >= 0) visual.layer = unitLayer;
        var body = visual.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var collider = visual.AddComponent<CircleCollider2D>();
        float scale = TroopConstants.SoldierRadius * 2f;
        collider.radius = TroopConstants.SoldierRadius / scale;
        var renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = soldier.Profile != null && soldier.Profile.Sprite != null
            ? soldier.Profile.Sprite
            : SoldierSpriteLibrary.Square;
        renderer.sharedMaterial = SoldierSpriteLibrary.ModelMaterial;
        renderer.sortingLayerName = "Unit";
        renderer.sortingOrder = 1;
        renderer.color = _troop.ArmyColor;
        visual.transform.localScale = Vector3.one * scale;
        return renderer;
    }
}