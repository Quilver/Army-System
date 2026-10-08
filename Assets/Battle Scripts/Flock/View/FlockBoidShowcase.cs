using System.Collections.Generic;
using UnityEngine;

public sealed class FlockBoidShowcase : MonoBehaviour
{
    [SerializeField] BoidProfile _squareProfile;
    [SerializeField] BoidProfile _triangleProfile;
    [SerializeField] BoidProfile _circleProfile;

    Flock _leftFlock;
    Flock _lowerLeftFlock;
    Flock _rightFlock;
    FlockProfile _flockProfile;
    bool _ownsSquareProfile;
    bool _ownsTriangleProfile;
    bool _ownsCircleProfile;
    bool _ownsFlockProfile;

    void Start()
    {
        EnsureProfiles();
        EnsureFlockProfile();
        _leftFlock = CreateFlock("Friendly Flock A", 0, new Vector2(-5f, 4.3f));
        _lowerLeftFlock = CreateFlock("Friendly Flock B", 0, new Vector2(-5f, -4.3f));
        _rightFlock = CreateFlock("Enemy Flock", 1, new Vector2(5f, 0f));
        IssueOrders();
    }

    void EnsureProfiles()
    {
        EnsureProfile(ref _squareProfile, BoidSpriteLibrary.Square, ref _ownsSquareProfile);
        EnsureProfile(ref _triangleProfile, BoidSpriteLibrary.Triangle, ref _ownsTriangleProfile);
        EnsureProfile(ref _circleProfile, BoidSpriteLibrary.Circle, ref _ownsCircleProfile);
    }

    void EnsureFlockProfile()
    {
        if (_flockProfile != null) return;
        _flockProfile = FlockProfile.CreateRuntimeProfile(100f, new List<FlockProfileCount>
        {
            new() { Profile = _squareProfile, Count = 12 },
            new() { Profile = _triangleProfile, Count = 6 },
            new() { Profile = _circleProfile, Count = 8 }
        }, 102f * 0.052f, 7.5f);
        _ownsFlockProfile = true;
    }

    static void EnsureProfile(ref BoidProfile profile, Sprite fallbackSprite, ref bool ownsProfile)
    {
        if (profile == null)
        {
            profile = BoidProfile.CreateRuntimeProfile(fallbackSprite);
            ownsProfile = true;
        }
        else
        {
            profile.SetSpriteIfMissing(fallbackSprite);
        }
    }

    Flock CreateFlock(string flockName, int team, Vector2 position)
    {
        var root = new GameObject(flockName);
        root.transform.position = position;
        var flock = root.AddComponent<Flock>();
        flock.Configure(team, _flockProfile);
        return flock;
    }

    void IssueOrders()
    {
        if (_leftFlock == null || _lowerLeftFlock == null || _rightFlock == null) return;
        _leftFlock.OrderLine(new Vector2(-0.15f, 4.3f), Vector2.up, 7.5f, new Vector2(0.15f, 0f));
        _lowerLeftFlock.OrderLine(new Vector2(-0.15f, -4.3f), Vector2.up, 7.5f, new Vector2(0.15f, 0f));
        _rightFlock.OrderLine(new Vector2(0.15f, 0f), Vector2.up, 7.5f, new Vector2(-0.15f, 0f));
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(16f, 16f, 380f, 130f), GUI.skin.box);
        GUILayout.Label("FLOCK + BOID TEST SCENE");
        GUILayout.Label("Two friendly mixed flocks face one enemy flock.");
        GUILayout.Label("Profiles use square, triangle, and circle sprites.");
        GUILayout.Label("Boids steer toward their line, align, cohere, and separate.");
        if (_leftFlock != null && _rightFlock != null)
            GUILayout.Label($"Shock A: {_leftFlock.Shock:0}/{_leftFlock.Morale:0}   Enemy: {_rightFlock.LivingBoidCount} boids");
        GUILayout.EndArea();
    }

    void OnDestroy()
    {
        if (_ownsSquareProfile) Destroy(_squareProfile);
        if (_ownsTriangleProfile) Destroy(_triangleProfile);
        if (_ownsCircleProfile) Destroy(_circleProfile);
        if (_ownsFlockProfile) Destroy(_flockProfile);
    }
}
