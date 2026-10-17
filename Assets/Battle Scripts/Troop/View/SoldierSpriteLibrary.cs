using UnityEngine;

public static class SoldierSpriteLibrary
{
    const int Resolution = 32;

    static Sprite _square;
    static Sprite _triangle;
    static Sprite _circle;
    static Material _modelMaterial;

    public static Sprite Square => _square != null ? _square : _square = Create("Infantry Square", IsSquare);
    public static Sprite Triangle => _triangle != null ? _triangle : _triangle = Create("Cavalry Triangle", IsTriangle);
    public static Sprite Circle => _circle != null ? _circle : _circle = Create("Archer Circle", IsCircle);
    public static Material ModelMaterial
    {
        get
        {
            if (_modelMaterial == null)
                _modelMaterial = Resources.Load<Material>("Battle prefabs/Model prefabs/ModelMat");
            return _modelMaterial;
        }
    }

    static Sprite Create(string spriteName, System.Func<int, int, bool> containsPixel)
    {
        var texture = new Texture2D(Resolution, Resolution, TextureFormat.RGBA32, false)
        {
            name = $"{spriteName} Texture",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };

        var pixels = new Color[Resolution * Resolution];
        for (int y = 0; y < Resolution; y++)
        for (int x = 0; x < Resolution; x++)
            pixels[y * Resolution + x] = containsPixel(x, y) ? Color.white : Color.clear;

        texture.SetPixels(pixels);
        texture.Apply(false, true);
        var sprite = UnityEngine.Sprite.Create(
            texture,
            new Rect(0f, 0f, Resolution, Resolution),
            new Vector2(0.5f, 0.5f),
            Resolution,
            0,
            SpriteMeshType.Tight);
        sprite.name = spriteName;
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    static bool IsSquare(int x, int y) => x >= 4 && x < 28 && y >= 4 && y < 28;

    static bool IsCircle(int x, int y)
    {
        float dx = x - 15.5f;
        float dy = y - 15.5f;
        return dx * dx + dy * dy <= 12f * 12f;
    }

    static bool IsTriangle(int x, int y)
    {
        if (y < 3 || y > 28) return false;
        float height = (y - 3f) / 25f;
        float halfWidth = 1f + (1f - height) * 12f;
        return Mathf.Abs(x - 15.5f) <= halfWidth;
    }
}
