using System.Collections.Generic;
using UnityEngine;

public sealed class TroopSoldierShowcase : MonoBehaviour
{
    [SerializeField] SoldierProfile _squareProfile;
    [SerializeField] SoldierProfile _triangleProfile;
    [SerializeField] SoldierProfile _circleProfile;

    Troop _leftTroop;
    Troop _lowerLeftTroop;
    Troop _rightTroop;
    TroopProfile _troopProfile;
    bool _ownsSquareProfile;
    bool _ownsTriangleProfile;
    bool _ownsCircleProfile;
    bool _ownsTroopProfile;

    void Start()
    {
        EnsureProfiles();
        EnsureTroopProfile();
        _leftTroop = CreateTroop("Friendly Troop A", 0, new Vector2(-15f, 13.5f));
        _lowerLeftTroop = CreateTroop("Friendly Troop B", 0, new Vector2(-15f, -13.5f));
        _rightTroop = CreateTroop("Enemy Troop", 1, new Vector2(15f, 0f));
        IssueOrders();
    }

    void EnsureProfiles()
    {
        EnsureProfile(ref _squareProfile, SoldierSpriteLibrary.Square, ref _ownsSquareProfile);
        EnsureProfile(ref _triangleProfile, SoldierSpriteLibrary.Triangle, ref _ownsTriangleProfile);
        EnsureProfile(ref _circleProfile, SoldierSpriteLibrary.Circle, ref _ownsCircleProfile);
    }

    void EnsureTroopProfile()
    {
        if (_troopProfile != null) return;
        _troopProfile = TroopProfile.CreateRuntimeProfile(100f, new List<TroopProfileCount>
        {
            new() { Profile = _squareProfile, Count = 12 },
            new() { Profile = _triangleProfile, Count = 6 },
            new() { Profile = _circleProfile, Count = 8 }
        }, 16.5f, 22f);
        _ownsTroopProfile = true;
    }

    static void EnsureProfile(ref SoldierProfile profile, Sprite fallbackSprite, ref bool ownsProfile)
    {
        if (profile == null)
        {
            profile = SoldierProfile.CreateRuntimeProfile(fallbackSprite);
            ownsProfile = true;
        }
        else
        {
            profile.SetSpriteIfMissing(fallbackSprite);
        }
    }

    Troop CreateTroop(string troopName, int team, Vector2 position)
    {
        var root = new GameObject(troopName);
        root.transform.position = position;
        var troop = root.AddComponent<Troop>();
        troop.Configure(team, _troopProfile);
        return troop;
    }

    void IssueOrders()
    {
        if (_leftTroop == null || _lowerLeftTroop == null || _rightTroop == null) return;
        _leftTroop.OrderLine(new Vector2(-0.5f, 13.5f), Vector2.up, 22f, new Vector2(0.5f, 0f));
        _lowerLeftTroop.OrderLine(new Vector2(-0.5f, -13.5f), Vector2.up, 22f, new Vector2(0.5f, 0f));
        _rightTroop.OrderLine(new Vector2(0.5f, 0f), Vector2.up, 22f, new Vector2(-0.5f, 0f));
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(16f, 16f, 380f, 130f), GUI.skin.box);
        GUILayout.Label("TROOP + SOLDIER TEST SCENE");
        GUILayout.Label("Two friendly mixed troops face one enemy troop.");
        GUILayout.Label("Profiles use square, triangle, and circle sprites.");
        GUILayout.Label("Soldiers steer toward their line, align, cohere, and separate.");
        if (_leftTroop != null && _rightTroop != null)
            GUILayout.Label($"Shock A: {_leftTroop.Shock:0}/{_leftTroop.Morale:0}   Enemy: {_rightTroop.LivingSoldierCount} soldiers");
        GUILayout.EndArea();
    }

    void OnDestroy()
    {
        if (_ownsSquareProfile) Destroy(_squareProfile);
        if (_ownsTriangleProfile) Destroy(_triangleProfile);
        if (_ownsCircleProfile) Destroy(_circleProfile);
        if (_ownsTroopProfile) Destroy(_troopProfile);
    }
}
