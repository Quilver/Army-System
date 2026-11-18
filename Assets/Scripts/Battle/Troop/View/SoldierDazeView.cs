using System.Collections.Generic;
using UnityEngine;

// The dazed look (SU-182): while its troop is dazed, every living soldier has StarCount small yellow stars circling it,
// like a cartoon knock on the head. The ring turns at TurnsPerSecond (each soldier starts at its own angle, so a troop
// does not spin in step), the stars twinkle and turn on themselves, and they fade out over the last FadeSeconds of the
// daze. Time stands still while the battle is paused. One vertex-coloured mesh per troop in world space
// (Resources/Shaders/TroopOverlayColor.shader) on the Unit sorting layer above the soldiers, rebuilt each frame only
// while the troop is dazed. Owned and refreshed by TroopSoldierViews.
public sealed class SoldierDazeView
{
    public const int StarCount = 3;
    const int StarPoints = 4;                   // four-pointed stars
    const float OrbitRadius = 0.42f;            // world units from the soldier's centre (a soldier is 0.3 in radius)
    const float StarOuterRadius = 0.14f;
    const float StarInnerRadius = 0.05f;
    const float TurnsPerSecond = 0.75f;
    const float SpinTurnsPerSecond = 1.5f;      // each star also turns on itself
    const float TwinkleHz = 3f;
    const float TwinkleDepth = 0.3f;            // the stars shrink by up to this fraction as they twinkle
    public const float FadeSeconds = 1f;
    const float GoldenAngle = 2.399963f;        // radians; spreads the soldiers' starting angles
    const string ShaderPath = "Shaders/TroopOverlayColor";
    const string SortingLayer = "Unit";
    const int SortingOrder = 3;                 // above the living soldiers (1) and projectiles (2)
    static readonly Color StarColour = new(1f, 0.86f, 0.2f, 1f);

    readonly List<Vector3> _vertices = new();
    readonly List<Color> _colors = new();
    readonly List<int> _triangles = new();
    GameObject _root;
    Mesh _mesh;
    Material _material;
    float _time;

    // Read by tests: what the last Refresh built.
    internal int VertexCount => _vertices.Count;
    internal int StarsDrawn => _vertices.Count / (StarPoints * 2 + 1);
    internal bool IsVisible => _root != null && _root.activeSelf;

    // The stars' opacity with this many seconds of daze left: full until the last FadeSeconds, then fading to nothing.
    public static float Opacity(float dazedRemaining) =>
        FadeSeconds > 0f ? Mathf.Clamp01(dazedRemaining / FadeSeconds) : (dazedRemaining > 0f ? 1f : 0f);

    public void Refresh(Troop troop, IReadOnlyList<SoldierAgentState> soldiers, float deltaTime)
    {
        _vertices.Clear();
        _colors.Clear();
        _triangles.Clear();
        if (troop == null || soldiers == null || !troop.IsDazed)
        {
            _time = 0f;
            SetVisible(false);
            return;
        }

        _time += Mathf.Max(0f, deltaTime);
        Color colour = StarColour;
        colour.a *= Opacity(troop.DazedRemaining);
        Transform pivot = troop.transform;
        const float fullTurn = 2f * Mathf.PI;
        foreach (var soldier in soldiers)
        {
            if (soldier == null || !soldier.IsAlive) continue;
            Vector3 centre = pivot.TransformPoint(soldier.Position);
            float phase = soldier.Id * GoldenAngle;
            float ring = phase + _time * TurnsPerSecond * fullTurn;
            for (int i = 0; i < StarCount; i++)
            {
                float angle = ring + i * fullTurn / StarCount;
                Vector3 at = centre + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * OrbitRadius;
                float twinkle = 1f - TwinkleDepth * (0.5f + 0.5f * Mathf.Sin((_time * TwinkleHz + i / (float)StarCount) * fullTurn + phase));
                AddStar(at, StarOuterRadius * twinkle, StarInnerRadius * twinkle,
                    phase + _time * SpinTurnsPerSecond * fullTurn, colour);
            }
        }

        if (_vertices.Count == 0)
        {
            SetVisible(false);
            return;
        }
        EnsureObjects();
        if (_mesh == null) return;
        _mesh.Clear();
        _mesh.SetVertices(_vertices);
        _mesh.SetColors(_colors);
        _mesh.SetTriangles(_triangles, 0);
        _mesh.RecalculateBounds();
        SetVisible(true);
    }

    public void Hide() => SetVisible(false);

    public void Destroy()
    {
        TroopObjects.Destroy(_mesh);
        TroopObjects.Destroy(_material);
        TroopObjects.Destroy(_root);
        _mesh = null;
        _material = null;
        _root = null;
    }

    // A star of StarPoints points: a centre vertex and a rim alternating between the points and the notches between them.
    void AddStar(Vector3 centre, float outer, float inner, float rotation, Color colour)
    {
        int first = _vertices.Count;
        _vertices.Add(centre);
        _colors.Add(colour);
        int rim = StarPoints * 2;
        for (int k = 0; k < rim; k++)
        {
            float angle = rotation + k * Mathf.PI / StarPoints;
            float radius = (k & 1) == 0 ? outer : inner;
            _vertices.Add(centre + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius);
            _colors.Add(colour);
        }
        for (int k = 0; k < rim; k++)
        {
            _triangles.Add(first);
            _triangles.Add(first + 1 + k);
            _triangles.Add(first + 1 + (k + 1) % rim);
        }
    }

    void SetVisible(bool visible)
    {
        if (_root != null && _root.activeSelf != visible) _root.SetActive(visible);
    }

    void EnsureObjects()
    {
        if (_root != null) return;
        Shader shader = Resources.Load<Shader>(ShaderPath);
        if (shader == null) return;
        _material = new Material(shader) { name = "Dazed Stars", hideFlags = HideFlags.DontSave };
        _root = new GameObject("Dazed Stars");
        _mesh = new Mesh { name = "Dazed Stars" };
        _mesh.MarkDynamic();
        _root.AddComponent<MeshFilter>().sharedMesh = _mesh;
        var renderer = _root.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = _material;
        renderer.sortingLayerName = SortingLayer;
        renderer.sortingOrder = SortingOrder;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }
}
