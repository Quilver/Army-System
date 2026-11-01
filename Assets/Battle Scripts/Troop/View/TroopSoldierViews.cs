using System.Collections.Generic;
using UnityEngine;

// One sprite GameObject (with a kinematic body and collider) per soldier, kept in step with the soldier simulation.
// Renderer i always belongs to soldier i of the roster.
//   Living soldiers  army colour; emission flashes on a clash, and turns grey at a third of its luminosity while the
//                    soldier is fatigued (HP <= half MaxHP). Injured soldiers above that keep the full glow.
//   Hit soldiers     squash and stretch along the push direction for HitSquashSeconds after taking melee damage:
//                    squashed in, then the back stretches out (wider and longer), then back to normal. The shape change
//                    is done in the model shader's vertex stage (_HitDir, _Deform).
//   Dead soldiers    stay where they fell for CorpseSeconds: no emission (black mask), drawn behind the living, army
//                    colour at half saturation, opacity fading from 1 to CorpseEndAlpha; then hidden. Corpses have no
//                    collider and no physics body.
public sealed class TroopSoldierViews
{
    const float CorpseSeconds = 10f;
    const float CorpseEndAlpha = 0.6f;
    const float CorpseSaturation = 0.5f;
    const int LivingSortingOrder = 1;
    const int CorpseSortingOrder = -1;       // behind the living soldiers on the Unit sorting layer
    const string CorpseShaderName = "Unlit/ModelCorpseShader";
    static readonly int EmissionMaskId = Shader.PropertyToID("_Emission");
    static readonly int MaskColorId = Shader.PropertyToID("_Color");
    static Material _corpseMaterial;
    const float HitSquashSeconds = 0.4f;
    static readonly int HitDirId = Shader.PropertyToID("_HitDir");
    static readonly int DeformId = Shader.PropertyToID("_Deform");

    readonly Troop _troop;
    readonly List<SpriteRenderer> _renderers = new();
    readonly List<SoldierGlow> _glow = new();     // emission state last applied to each renderer
    readonly List<float> _deadAge = new();        // seconds since the soldier died; negative while alive
    readonly List<float> _hitAge = new();         // seconds into the hit squash and stretch; negative when not playing
    readonly MaterialPropertyBlock _hitBlock = new();
    readonly ClashPulseView _pulse = new();

    public TroopSoldierViews(Troop troop)
    {
        _troop = troop;
    }

    public void Create()
    {
        DestroyAll();
        foreach (var soldier in _troop.Roster.SoldierList)
        {
            _renderers.Add(CreateSoldierObject(soldier));
            _glow.Add(SoldierGlow.Healthy);
            _deadAge.Add(-1f);
            _hitAge.Add(-1f);
        }
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

            if (!soldier.IsAlive)
            {
                UpdateCorpse(i, renderer, armyColor, deltaTime);
                continue;
            }
            renderer.enabled = true;
            renderer.color = armyColor;
            UpdateEmission(i, renderer, soldier, deltaTime);
            UpdateHitSquash(i, renderer, deltaTime);
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

        if (!soldier.IsAlive) return;
        soldier.BeginClashColorPulse(duration);
        _glow[index] = Glow(soldier);
        _pulse.Apply(renderer, soldier.ClashColorPulseProgress, _glow[index]);
    }

    // Corpses whose time is up stay hidden.
    public void SetVisible(bool visible)
    {
        for (int i = 0; i < _renderers.Count; i++)
            if (_renderers[i] != null) _renderers[i].enabled = visible && _deadAge[i] < CorpseSeconds;
    }

    public void DestroyAll()
    {
        foreach (var renderer in _renderers)
            if (renderer != null) TroopObjects.Destroy(renderer.gameObject);
        _renderers.Clear();
        _glow.Clear();
        _deadAge.Clear();
        _hitAge.Clear();
    }

    // Clash flash; fatigued soldiers glow grey at a third of the luminosity. The property block is only rewritten while
    // flashing, when a flash ends, or when the soldier's glow changes.
    void UpdateEmission(int index, SpriteRenderer renderer, SoldierAgentState soldier, float deltaTime)
    {
        SoldierGlow glow = Glow(soldier);
        bool pulseEnded = soldier.TickClashColorPulse(deltaTime);
        if (soldier.IsClashColorPulsing)
            _pulse.Apply(renderer, soldier.ClashColorPulseProgress, glow);
        else if (pulseEnded || _glow[index] != glow)
            _pulse.Apply(renderer, 1f, glow);
        _glow[index] = glow;
    }

    public void PlayHitSquash(SoldierAgentState soldier, Vector2 worldPushDirection)
    {
        int index = _troop.Roster.SoldierList.IndexOf(soldier);
        if (index < 0 || index >= _renderers.Count || _renderers[index] == null || !soldier.IsAlive) return;
        if (worldPushDirection.sqrMagnitude < 1e-8f) worldPushDirection = Vector2.up;
        _hitAge[index] = 0f;
        SetHitBlock(_renderers[index], worldPushDirection.normalized, HitDeform(0f), true);
    }

    void UpdateHitSquash(int index, SpriteRenderer renderer, float deltaTime)
    {
        if (_hitAge[index] < 0f) return;
        _hitAge[index] += deltaTime;
        if (_hitAge[index] >= HitSquashSeconds)
        {
            _hitAge[index] = -1f;
            SetHitBlock(renderer, Vector2.zero, 0f, false);
            return;
        }
        SetHitBlock(renderer, Vector2.zero, HitDeform(_hitAge[index] / HitSquashSeconds), false);
    }

    void SetHitBlock(SpriteRenderer renderer, Vector2 direction, float deform, bool setDirection)
    {
        renderer.GetPropertyBlock(_hitBlock);
        if (setDirection) _hitBlock.SetVector(HitDirId, new Vector4(direction.x, direction.y, 0f, 0f));
        _hitBlock.SetFloat(DeformId, deform);
        renderer.SetPropertyBlock(_hitBlock);
    }

    // -1 = fully squashed, +1 = fully stretched. Squash in over the first 20%, spring out to the stretch by 50%, settle.
    public static float HitDeform(float t)
    {
        if (t < 0.2f) return -Mathf.SmoothStep(0f, 1f, t / 0.2f);
        if (t < 0.5f) return Mathf.Lerp(-1f, 1f, Mathf.SmoothStep(0f, 1f, (t - 0.2f) / 0.3f));
        return 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.5f) / 0.5f));
    }

    static SoldierGlow Glow(SoldierAgentState soldier) =>
        soldier.LifeState == SoldierLifeState.Fatigued ? SoldierGlow.Fatigued : SoldierGlow.Healthy;

    // A dead soldier stays where it fell (its sprite is no longer moved with the troop) and fades for CorpseSeconds.
    void UpdateCorpse(int index, SpriteRenderer renderer, Color armyColor, float deltaTime)
    {
        if (_deadAge[index] < 0f) BeginCorpse(index, renderer);
        else _deadAge[index] += deltaTime;

        bool visible = _deadAge[index] < CorpseSeconds;
        renderer.enabled = visible;
        if (!visible) return;

        Color color = Color.Lerp(ClashPulseView.Grey(armyColor), armyColor, CorpseSaturation);
        color.a = Mathf.Lerp(1f, CorpseEndAlpha, _deadAge[index] / CorpseSeconds);
        renderer.color = color;
    }

    void BeginCorpse(int index, SpriteRenderer renderer)
    {
        _deadAge[index] = 0f;
        _hitAge[index] = -1f;
        SetHitBlock(renderer, Vector2.zero, 0f, false);
        renderer.sortingOrder = CorpseSortingOrder;
        // Corpses take no part in physics: no collider, and the kinematic body leaves the simulation.
        if (renderer.TryGetComponent(out Collider2D collider)) collider.enabled = false;
        if (renderer.TryGetComponent(out Rigidbody2D body)) body.simulated = false;
        Material corpse = CorpseMaterial(renderer.sharedMaterial);
        if (corpse != null) renderer.sharedMaterial = corpse;
        _pulse.Apply(renderer, 1f, SoldierGlow.Dead);
        _glow[index] = SoldierGlow.Dead;
    }

    // Shared corpse material: the alpha-blended copy of the model shader, with the living material's emission mask and a
    // black mask colour. Null if the shader is missing (the corpse then keeps the opaque material and cannot fade).
    static Material CorpseMaterial(Material living)
    {
        if (_corpseMaterial != null) return _corpseMaterial;
        Shader shader = Shader.Find(CorpseShaderName);
        if (shader == null) return null;
        _corpseMaterial = new Material(shader) { name = "Soldier Corpse", hideFlags = HideFlags.DontSave };
        if (living != null && living.HasProperty(EmissionMaskId))
            _corpseMaterial.SetTexture(EmissionMaskId, living.GetTexture(EmissionMaskId));
        _corpseMaterial.SetColor(MaskColorId, Color.black);
        return _corpseMaterial;
    }

    SpriteRenderer CreateSoldierObject(SoldierAgentState soldier)
    {
        var visual = new GameObject($"Soldier {soldier.Id}");
        visual.AddComponent<SoldierView>().Bind(_troop, soldier);   // Inspector shows this soldier's HP (SoldierViewEditor)
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
        renderer.sprite = soldier.Character != null && soldier.Character.SoldierSprite != null
            ? soldier.Character.SoldierSprite
            : SoldierSpriteLibrary.Square;
        renderer.sharedMaterial = SoldierSpriteLibrary.ModelMaterial;
        renderer.sortingLayerName = "Unit";
        renderer.sortingOrder = LivingSortingOrder;
        renderer.color = _troop.ArmyColor;
        visual.transform.localScale = Vector3.one * scale;
        return renderer;
    }
}