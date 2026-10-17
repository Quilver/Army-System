using UnityEngine;

public sealed class TroopProfileSpriteDefaults : MonoBehaviour
{
    [SerializeField] SoldierProfile _squareProfile;
    [SerializeField] SoldierProfile _triangleProfile;
    [SerializeField] SoldierProfile _circleProfile;

    void Awake()
    {
        if (_squareProfile != null) _squareProfile.SetSpriteIfMissing(SoldierSpriteLibrary.Square);
        if (_triangleProfile != null) _triangleProfile.SetSpriteIfMissing(SoldierSpriteLibrary.Triangle);
        if (_circleProfile != null) _circleProfile.SetSpriteIfMissing(SoldierSpriteLibrary.Circle);
    }
}
