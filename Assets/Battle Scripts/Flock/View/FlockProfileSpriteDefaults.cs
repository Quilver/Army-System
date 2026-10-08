using UnityEngine;

public sealed class FlockProfileSpriteDefaults : MonoBehaviour
{
    [SerializeField] BoidProfile _squareProfile;
    [SerializeField] BoidProfile _triangleProfile;
    [SerializeField] BoidProfile _circleProfile;

    void Awake()
    {
        if (_squareProfile != null) _squareProfile.SetSpriteIfMissing(BoidSpriteLibrary.Square);
        if (_triangleProfile != null) _triangleProfile.SetSpriteIfMissing(BoidSpriteLibrary.Triangle);
        if (_circleProfile != null) _circleProfile.SetSpriteIfMissing(BoidSpriteLibrary.Circle);
    }
}
