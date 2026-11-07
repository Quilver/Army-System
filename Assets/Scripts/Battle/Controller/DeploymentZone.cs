using System;
using System.Collections.Generic;
using UnityEngine;

// The area where an army's troops start the battle and, during the deployment phase, where the player may place them.
// The area is this object's unit square: scale it to size (the sprite shows it while deploying).
// At the start of the scene it spawns one troop per character list in BattleReport.DeployedTroops (TroopSource, set by
// the campaign bridge): the level's default troops on the level's DataManager, or the roster chosen in battle
// preparation. Each troop gets its characters and sets its own frontage from them (SU-117). Troops are laid out in rows
// inside the area, facing the enemy, Idle (SU-101).
// While the battle is deploying, player Move and Line orders place troops straight at their spot, kept inside the area
// (ITroopPlacement through TroopPlayerOrders.Placement): a Move leaves them Idle, a Line puts them in that line.
// When deployment starts, the player's zone centres the battle camera on itself, the camera a little below the zone's
// centre so the zone sits above the troop cards and buttons along the bottom of the screen (the camera used to open
// with the zone at the bottom edge). CameraControls then keeps the camera inside its bounds as usual.
[DefaultExecutionOrder(-100)]
public sealed class DeploymentZone : MonoBehaviour, ITroopPlacement
{
    // Supplies the troops to deploy (BattleReport.DeployedTroops), each as its characters, leader first. Set by the
    // campaign bridge (Tween), which the battle may not reference. Null deploys nothing.
    public static Func<IReadOnlyList<IReadOnlyList<CharacterProfile>>> TroopSource;

    static readonly List<DeploymentZone> Zones = new();

    [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("armyData"),
     Tooltip("The army whose troops deploy here; spawned troops become its children.")]
    Army _army;
    [SerializeField, Tooltip("Team id of the spawned troops (0 = the player team in TroopPlayerOrders).")]
    int _teamId;
    [SerializeField, Tooltip("Prefab each troop is made from. Empty: a plain troop with the Resources troop visualiser.")]
    Troop _troopPrefab;
    [SerializeField, Min(0f), Tooltip("Gap between spawned troops (units).")]
    float _gap = 2f;
    [SerializeField, Tooltip("Shows the area while deploying. Defaults to the SpriteRenderer on this object. Every other " +
        "renderer under the zone (its border) shows and hides with it.")]
    SpriteRenderer _areaRenderer;

    const string VisualiserPrefabPath = "Troop and Character Profiles/Troop Visualiser";
    const float CameraBelowZone = 0.15f;   // camera offset below the zone's centre, as a fraction of its orthographic size

    readonly List<Troop> _spawned = new();
    // The area and everything drawn under the zone (the Border child): shown only while deploying.
    readonly List<Renderer> _renderers = new();
    Battle _battle;

    public Army Army => _army;
    public IReadOnlyList<Troop> SpawnedTroops => _spawned;

    // The zone the army deploys in, or null.
    public static DeploymentZone For(Army army)
    {
        if (army == null) return null;
        foreach (var zone in Zones)
            if (zone != null && zone._army == army) return zone;
        return null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ClearStatics()
    {
        Zones.Clear();
        TroopSource = null;
    }

    // The troops this zone deploys (BattleReport.DeployedTroops).
    public IReadOnlyList<IReadOnlyList<CharacterProfile>> TroopsToDeploy() =>
        TroopSource?.Invoke() ?? Array.Empty<IReadOnlyList<CharacterProfile>>();

    void Awake()
    {
        if (_areaRenderer == null) _areaRenderer = GetComponent<SpriteRenderer>();
        CollectRenderers();
        if (!Zones.Contains(this)) Zones.Add(this);
        Spawn(TroopsToDeploy());
    }

    void Start()
    {
        _battle = Battle.Instance;
        if (_battle != null) _battle.PhaseChanged += OnPhaseChanged;
        OnPhaseChanged(_battle != null ? _battle.Phase : BattlePhase.Fighting);
        if (_battle != null && _battle.IsDeploying && _army != null && _army == _battle.player) FrameCamera(Camera.main);
    }

    // Moves an orthographic camera to CameraFocus, keeping its depth.
    void FrameCamera(Camera camera)
    {
        if (camera == null || !camera.orthographic) return;
        Vector2 focus = CameraFocus(camera.orthographicSize);
        Vector3 position = camera.transform.position;
        camera.transform.position = new Vector3(focus.x, focus.y, position.z);
    }

    // Where the camera centres to show this zone: CameraBelowZone x orthographicSize below the zone's centre, so the
    // zone shows a little above the middle of the screen. Internal for tests.
    internal Vector2 CameraFocus(float orthographicSize) =>
        (Vector2)transform.position + Vector2.down * (orthographicSize * CameraBelowZone);

    void OnDestroy()
    {
        Zones.Remove(this);
        if (_battle != null) _battle.PhaseChanged -= OnPhaseChanged;
        SetPlacement(false);
    }

    void OnPhaseChanged(BattlePhase phase) => ApplyPhase(phase);

    // Shows the zone (area and border) and turns placement on only while deploying. Internal for tests.
    internal void ApplyPhase(BattlePhase phase)
    {
        bool deploying = phase == BattlePhase.Deployment;
        if (_renderers.Count == 0) CollectRenderers();
        foreach (var renderer in _renderers)
            if (renderer != null) renderer.enabled = deploying;
        SetPlacement(deploying);
    }

    // The area renderer and every renderer below the zone, such as the border outline, which used to stay on screen
    // after deployment. Collected before troops spawn and skipping anything inside a troop, in case a troop is ever
    // parented under the zone.
    void CollectRenderers()
    {
        _renderers.Clear();
        if (_areaRenderer == null) _areaRenderer = GetComponent<SpriteRenderer>();
        if (_areaRenderer != null) _renderers.Add(_areaRenderer);
        foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            if (!_renderers.Contains(renderer) && renderer.GetComponentInParent<Troop>(true) == null) _renderers.Add(renderer);
    }

    // Read by tests: whether any part of the zone is drawn.
    internal bool IsShown
    {
        get
        {
            foreach (var renderer in _renderers)
                if (renderer != null && renderer.enabled) return true;
            return false;
        }
    }

    void SetPlacement(bool deploying)
    {
        foreach (var orders in FindObjectsByType<TroopPlayerOrders>(FindObjectsSortMode.None))
        {
            if (deploying) orders.Placement = this;
            else if (ReferenceEquals(orders.Placement, this)) orders.Placement = null;
        }
    }

    // --- Area ---

    // Facing of troops deployed here: towards the enemy army, otherwise along this object's up.
    public Vector2 DeployFacing()
    {
        Army enemy = _army != null ? _army.Enemy : null;
        if (enemy != null)
        {
            Vector2 sum = Vector2.zero;
            int count = 0;
            foreach (var troop in enemy.Troops)
            {
                sum += (Vector2)troop.transform.position;
                count++;
            }
            if (count > 0)
            {
                Vector2 toEnemy = sum / count - (Vector2)transform.position;
                if (toEnemy.sqrMagnitude > 1e-4f) return toEnemy.normalized;
            }
        }
        return ((Vector2)transform.up).normalized;
    }

    public bool Contains(Vector2 world, float inset = 0f) => (Clamp(world, inset) - world).sqrMagnitude < 1e-6f;

    // The nearest point to world inside the area, at least inset (units) from its edges where the area is big enough.
    public Vector2 Clamp(Vector2 world, float inset = 0f)
    {
        Vector2 local = transform.InverseTransformPoint(world);
        Vector2 scale = transform.lossyScale;
        float halfX = Mathf.Max(0f, 0.5f - inset / Mathf.Max(1e-4f, Mathf.Abs(scale.x)));
        float halfY = Mathf.Max(0f, 0.5f - inset / Mathf.Max(1e-4f, Mathf.Abs(scale.y)));
        local.x = Mathf.Clamp(local.x, -halfX, halfX);
        local.y = Mathf.Clamp(local.y, -halfY, halfY);
        return transform.TransformPoint(local);
    }

    static float Inset(Troop troop) => troop.Radius * TroopTuning.Active.BlobRadiusScale;

    // --- ITroopPlacement (deployment orders) ---

    public bool Place(Troop troop, Vector2 center, Vector2 facing, float length, bool holdLine)
    {
        if (troop == null || troop.Army != _army) return false;
        if (_battle != null && !_battle.IsDeploying) return false;
        troop.PlaceAt(Clamp(center, Inset(troop)), facing, length, holdLine);
        return true;
    }

    // --- Spawning ---

    void Spawn(IReadOnlyList<IReadOnlyList<CharacterProfile>> troopCharacters)
    {
        if (troopCharacters == null || troopCharacters.Count == 0) return;
        Vector2 facing = DeployFacing();
        Vector2 right = new Vector2(facing.y, -facing.x);
        Vector2 centre = transform.position;

        // Each troop is made from its characters first: it determines its frontage, and so its size, as it spawns.
        var troops = new List<Troop>();
        foreach (var characters in troopCharacters)
        {
            if (!HasCharacter(characters)) continue;
            Troop troop = SpawnTroop(characters, centre);
            if (troop != null) troops.Add(troop);
        }
        if (troops.Count == 0) return;

        float width = 0f, depth = 0f;
        foreach (var troop in troops)
        {
            width = Mathf.Max(width, troop.NaturalLineLength + _gap);
            depth = Mathf.Max(depth, 2f * troop.Radius * TroopTuning.Active.BlobRadiusScale + _gap);
        }

        // Rows across the area's extent perpendicular to the facing, front row first.
        float span = AreaSpan(right);
        int perRow = Mathf.Clamp(Mathf.FloorToInt(span / Mathf.Max(0.1f, width)), 1, troops.Count);
        int rows = Mathf.CeilToInt(troops.Count / (float)perRow);
        for (int i = 0; i < troops.Count; i++)
        {
            int row = i / perRow;
            int inRow = Mathf.Min(perRow, troops.Count - row * perRow);
            int file = i % perRow;
            float x = (file - (inRow - 1) * 0.5f) * width;
            float y = ((rows - 1) * 0.5f - row) * depth;
            Troop troop = troops[i];
            Vector2 position = centre + right * x + facing * y;
            troop.PlaceAt(Clamp(position, troop.Radius * TroopTuning.Active.BlobRadiusScale), facing,
                troop.NaturalLineLength, holdLine: false);
            _spawned.Add(troop);
        }
    }

    static bool HasCharacter(IReadOnlyList<CharacterProfile> characters)
    {
        if (characters == null) return false;
        foreach (var character in characters)
            if (character != null) return true;
        return false;
    }

    // Length of the area along a direction (through its centre).
    float AreaSpan(Vector2 direction)
    {
        Vector2 x = transform.right * transform.lossyScale.x;
        Vector2 y = transform.up * transform.lossyScale.y;
        return Mathf.Abs(Vector2.Dot(x, direction)) + Mathf.Abs(Vector2.Dot(y, direction));
    }

    Troop SpawnTroop(IReadOnlyList<CharacterProfile> characters, Vector2 position)
    {
        Transform parent = _army != null ? _army.transform : null;
        Troop troop;
        if (_troopPrefab != null)
        {
            troop = Instantiate(_troopPrefab, parent);
        }
        else
        {
            var root = new GameObject("Troop");
            root.transform.SetParent(parent, false);
            var visualiser = Resources.Load<TroopVisualiser>(VisualiserPrefabPath);
            if (visualiser != null) Instantiate(visualiser, root.transform).name = visualiser.name;
            troop = root.AddComponent<Troop>();
        }
        troop.transform.position = position;
        troop.Configure(_teamId, characters);
        troop.name = troop.DisplayName;
        return troop;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.8f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        Gizmos.matrix = Matrix4x4.identity;
    }
}
