using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

// Soldier sprites (TroopSoldierViews): fatigued soldiers glow grey at a third of the luminosity, injured soldiers above
// that keep the full glow; dead soldiers leave a
// corpse for 10 s with no emission and no collider, drawn behind the living, fading to 0.6 opacity, then hidden
// (SU-32, SU-34, SU-35).
[Category("Scenario")]
public sealed class SoldierViewTests
{
    const float FrameTime = 1f / 30f;
    static readonly int ColorId = Shader.PropertyToID("_Color");
    readonly List<Object> _created = new();
    TroopCombatWorld _world;
    TroopTuning _tuning;

    [SetUp]
    public void SetUp()
    {
        _tuning = TroopTuning.CreateDefaults();
        TroopTuning.OverrideForTests(_tuning);
        _world = new GameObject("Test Combat World").AddComponent<TroopCombatWorld>();
        _world.SetSeed(12345);
        _world.Initialize();
        _created.Add(_world.gameObject);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var target in _created)
            if (target != null) Object.DestroyImmediate(target);
        _created.Clear();
        TroopTuning.OverrideForTests(null);
        _tuning?.DestroySections();
        _tuning = null;
    }

    Troop CreateTroop()
    {
        var weapon = MeleeWeaponProfile.CreateRuntimeProfile("Test Sword", MeleeWeaponClass.Sword, 0, 0,
            null, false);
        _created.Add(weapon);
        var character = CharacterProfile.CreateRuntimeProfile("Test", null, null, null, 12, 5,
            CombatStatLimits.Default, new CharacterEquipment(weapon, null, null), new SoldierStats(5, 5, 5, 5, 5, 5, 15));
        _created.Add(character);
        var profile = TroopProfile.CreateRuntimeProfile(new List<CharacterProfile> { character }, 5f);
        _created.Add(profile);
        var root = new GameObject("View Test Troop");
        _created.Add(root);
        var troop = root.AddComponent<Troop>();
        troop.Configure(0, profile);
        return troop;
    }

    void Simulate(float seconds)
    {
        int frames = Mathf.CeilToInt(seconds / FrameTime);
        for (int i = 0; i < frames; i++) _world.SimulateFrame(FrameTime);
    }

    static SpriteRenderer RendererOf(SoldierAgentState soldier)
    {
        foreach (var view in Object.FindObjectsByType<SoldierView>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (view.Soldier == soldier) return view.GetComponent<SpriteRenderer>();
        return null;
    }

    static Color Emission(SpriteRenderer renderer)
    {
        var block = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(block);
        // No override in the block means the material's own colour is shown.
        return block.HasColor(ColorId) ? block.GetColor(ColorId) : renderer.sharedMaterial.GetColor(ColorId);
    }

    static float Luminance(Color c) => 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;

    [Test]
    public void InjuredSoldier_NotFatigued_KeepsFullGlow()
    {
        Troop troop = CreateTroop();
        Simulate(0.1f);
        SoldierAgentState soldier = troop.Soldiers[0];
        SpriteRenderer renderer = RendererOf(soldier);
        Assert.IsNotNull(renderer, "Soldier sprite");
        Color healthy = renderer.sharedMaterial.GetColor(ColorId);

        soldier.ApplyMitigation(new MitigationResult(0, 0, 3, 0));   // 3 bleed of 15: injured, not fatigued
        Simulate(0.1f);
        Assert.AreEqual(SoldierLifeState.Active, soldier.LifeState);
        Color injured = Emission(renderer);
        Assert.AreEqual(healthy.r, injured.r, 1e-3f, "Full glow");
        Assert.AreEqual(healthy.g, injured.g, 1e-3f, "Full glow");
        Assert.AreEqual(healthy.b, injured.b, 1e-3f, "Full glow");
    }

    [Test]
    public void FatiguedSoldier_GlowsGrey_AtAThird()
    {
        Troop troop = CreateTroop();
        Simulate(0.1f);
        SoldierAgentState soldier = troop.Soldiers[0];
        SpriteRenderer renderer = RendererOf(soldier);
        Assert.IsNotNull(renderer, "Soldier sprite");
        Color healthy = renderer.sharedMaterial.GetColor(ColorId);

        soldier.ApplyMitigation(new MitigationResult(0, 0, 8, 0));   // 7 of 15 HP left: fatigued
        Simulate(0.1f);
        Assert.AreEqual(SoldierLifeState.Fatigued, soldier.LifeState);
        Color fatigued = Emission(renderer);
        Assert.AreEqual(fatigued.r, fatigued.g, 1e-3f, "Grey");
        Assert.AreEqual(fatigued.g, fatigued.b, 1e-3f, "Grey");
        Assert.AreEqual(Luminance(healthy) / 3f, Luminance(fatigued), Luminance(healthy) * 0.01f, "A third of the luminosity");
    }

    [TestCase("Unlit/ModelShader")]
    [TestCase("Unlit/ModelCorpseShader")]
    public void SoldierShaders_Compile(string shaderName)
    {
        Shader shader = Shader.Find(shaderName);
        Assert.IsNotNull(shader, shaderName);
        Assert.IsFalse(UnityEditor.ShaderUtil.ShaderHasError(shader), shaderName + " has compile errors");
    }

    [Test]
    public void HitSoldier_SquashesThenStretches_ThenReturnsToNormal()
    {
        Troop troop = CreateTroop();
        Simulate(0.1f);
        SoldierAgentState soldier = troop.Soldiers[0];
        SpriteRenderer renderer = RendererOf(soldier);
        Assert.IsNotNull(renderer, "Soldier sprite");
        var block = new MaterialPropertyBlock();
        int deformId = Shader.PropertyToID("_Deform");
        int directionId = Shader.PropertyToID("_HitDir");

        troop.PlayHitSquash(soldier, new Vector2(0f, 3f));
        renderer.GetPropertyBlock(block);
        Assert.AreEqual(new Vector4(0f, 1f, 0f, 0f), block.GetVector(directionId), "Push direction, normalised");

        Simulate(0.07f);    // 20% of 0.4 s: squashed
        renderer.GetPropertyBlock(block);
        Assert.Less(block.GetFloat(deformId), -0.5f, "Squashed in");

        Simulate(0.13f);    // 50%: stretched
        renderer.GetPropertyBlock(block);
        Assert.Greater(block.GetFloat(deformId), 0.5f, "Stretched out");

        Simulate(0.3f);     // done
        renderer.GetPropertyBlock(block);
        Assert.AreEqual(0f, block.GetFloat(deformId), 1e-5f, "Back to normal");
    }

    [Test]
    public void DeadSoldier_LeavesAFadingCorpse_ForTenSeconds()
    {
        Troop troop = CreateTroop();
        Simulate(0.1f);
        SoldierAgentState soldier = troop.Soldiers[0];
        SpriteRenderer renderer = RendererOf(soldier);
        Assert.IsNotNull(renderer, "Soldier sprite");

        soldier.ApplyMitigation(new MitigationResult(0, 0, 100, 0));
        Simulate(FrameTime);
        Vector3 fellAt = renderer.transform.position;
        Assert.IsTrue(renderer.enabled, "Corpse shown");
        Assert.Less(renderer.sortingOrder, 1, "Drawn behind the living");
        Color emission = Emission(renderer);
        Assert.AreEqual(0f, emission.r + emission.g + emission.b, 1e-4f, "No emission");
        Assert.Greater(renderer.color.a, 0.95f, "Starts opaque");
        Assert.IsFalse(renderer.GetComponent<Collider2D>().enabled, "Corpse has no collider");
        Assert.IsFalse(renderer.GetComponent<Rigidbody2D>().simulated, "Corpse leaves the physics simulation");

        Simulate(5f);
        Assert.IsTrue(renderer.enabled, "Still there at 5 s");
        Assert.AreEqual(0.8f, renderer.color.a, 0.03f, "Half way from 1 to 0.6");
        Assert.AreEqual(fellAt, renderer.transform.position, "Stays where it fell");

        Simulate(5.2f);
        Assert.IsFalse(renderer.enabled, "Gone after 10 s");
    }
}
