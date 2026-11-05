using System;
using System.Collections.Generic;
using UnityEngine;

// The area where an army's troops start the battle and, during the deployment phase, where the player may place them.
// The area is this object's unit square: scale it to size (the sprite shows it while deploying).
// At the start of the scene it spawns one troop per profile in BattleReport.DeployedTroops (TroopSource, set by the
// campaign bridge): the level's default troops on the level's DataManager, or the roster chosen in battle preparation.
// Troops are laid out in rows inside the area, facing the enemy.
// While the battle is deploying, player Move and Line orders place troops straight at their spot, kept inside the area
// (ITroopPlacement through TroopPlayerOrders.Placement).
[DefaultExecutionOrder(-100)]
public sealed class DeploymentZone : MonoBehaviour, ITroopPlacement
{
    // Supplies the troops to deploy (BattleReport.DeployedTroops). Set by the campaign bridge (Tween), which the battle
    // may not reference. Null deploys nothing.
    public static Func<IReadOnlyList<TroopProfile>> TroopSource;

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
    [SerializeField, Tooltip("Shows the area while deploying. Defaults to the SpriteRenderer on this object.")]
    SpriteRenderer _areaRenderer;

    const string VisualiserPrefabPath = "Troop and Character Profiles/Troop Visualiser";

    readonly List<Troop> _spawned = new();
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
    public IReadOnlyList<TroopProfile> TroopsToDeploy() => TroopSource?.Invoke() ?? Array.Empty<TroopProfile>();

    void Awake()
    {
        if (_areaRenderer == null) _areaRenderer = GetComponent<SpriteRenderer>();
        if (!Zones.Contains(this)) Zones.Add(this);
        Spawn(TroopsToDeploy());
    }

    void Start()
    {
        _battle = Battle.Instance;
        if (_battle != null) _battle.PhaseChanged += OnPhaseChanged;
        OnPhaseChanged(_battle != null ? _battle.Phase : BattlePhase.Fighting);
    }

    void OnDestroy()
    {
        Zones.Remove(this);
        if (_battle != null) _battle.PhaseChanged -= OnPhaseChanged;
        SetPlacement(false);
    }

    void OnPhaseChanged(BattlePhase phase)
    {
        bool deploying = phase == BattlePhase.Deployment;
        if (_areaRenderer != null) _areaRenderer.enabled = deploying;
        SetPlacement(deploying);
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

    public bool Place(Troop troop, Vector2 center, Vector2 facing, float length)
    {
        if (troop == null || troop.Army != _army) return false;
        if (_battle != null && !_battle.IsDeploying) return false;
        troop.PlaceAt(Clamp(center, Inset(troop)), facing, length);
        return true;
    }

    // --- Spawning ---

    void Spawn(IReadOnlyList<TroopProfile> profiles)
    {
        if (profiles == null || profiles.Count == 0) return;
        Vector2 facing = DeployFacing();
        Vector2 right = new Vector2(facing.y, -facing.x);

        float width = 0f, depth = 0f;
        foreach (var profile in profiles)
        {
            if (profile == null) continue;
            width = Mathf.Max(width, TroopTuning.Active.NaturalLineLength(profile.TroopRadius) + _gap);
            depth = Mathf.Max(depth, 2f * profile.TroopRadius * TroopTuning.Active.BlobRadiusScale + _gap);
        }

        // Rows across the area's extent perpendicular to the facing, front row first.
        float span = AreaSpan(right);
        int perRow = Mathf.Clamp(Mathf.FloorToInt(span / Mathf.Max(0.1f, width)), 1, profiles.Count);
        int rows = Mathf.CeilToInt(profiles.Count / (float)perRow);
        Vector2 centre = transform.position;
        for (int i = 0; i < profiles.Count; i++)
        {
            if (profiles[i] == null) continue;
            int row = i / perRow;
            int inRow = Mathf.Min(perRow, profiles.Count - row * perRow);
            int file = i % perRow;
            float x = (file - (inRow - 1) * 0.5f) * width;
            float y = ((rows - 1) * 0.5f - row) * depth;
            Troop troop = SpawnTroop(profiles[i], centre + right * x + facing * y, facing);
            if (troop != null) _spawned.Add(troop);
        }
    }

    // Length of the area along a direction (through its centre).
    float AreaSpan(Vector2 direction)
    {
        Vector2 x = transform.right * transform.lossyScale.x;
        Vector2 y = transform.up * transform.lossyScale.y;
        return Mathf.Abs(Vector2.Dot(x, direction)) + Mathf.Abs(Vector2.Dot(y, direction));
    }

    Troop SpawnTroop(TroopProfile profile, Vector2 position, Vector2 facing)
    {
        Transform parent = _army != null ? _army.transform : null;
        Troop troop;
        if (_troopPrefab != null)
        {
            troop = Instantiate(_troopPrefab, parent);
        }
        else
        {
            var root = new GameObject(profile.DisplayName);
            root.transform.SetParent(parent, false);
            var visualiser = Resources.Load<TroopVisualiser>(VisualiserPrefabPath);
            if (visualiser != null) Instantiate(visualiser, root.transform).name = visualiser.name;
            troop = root.AddComponent<Troop>();
        }
        troop.name = profile.DisplayName;
        troop.transform.position = position;
        troop.Configure(_teamId, profile);
        troop.PlaceAt(Clamp(position, troop.Radius * TroopTuning.Active.BlobRadiusScale), facing, troop.NaturalLineLength);
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
