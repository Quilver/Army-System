using UnityEngine;

// A point the armies fight over. Every living soldier inside the point's 2D collider pulls control toward its army;
// control moves at a rate set by the difference in soldiers (capped at 30) and is captured past triggerThreshold.
public class CapturePoint : MonoBehaviour
{
    public enum PointController
    {
        Neutral,
        Player,
        Enemy
    }

    public enum CapturePointType
    {
        Fortress,
        Village,
        Banner,
        Misc
    }

    const int MaxSoldierDifference = 30;
    const float CountInterval = 0.25f;

    [SerializeField] Color PlayerControlled, NeutralControlled, AIControlled;
    [SerializeField, Range(-1, 1)] float holder;
    [SerializeField, Range(0, 1)] float triggerThreshold = 0.8f;
    [SerializeField, Tooltip("Used when the point has no Collider2D.")] float _radius = 5f;

    // Raised when an army takes the point (Battle.player or Battle.enemy).
    public static System.Action<CapturePoint, Army> CapturedBy;
    public static event System.Action<CapturePoint, bool> PointCaptured;
    public event System.Action<bool> capturedBy;

    SpriteRenderer _sprite;
    Collider2D _area;
    int _soldierDifference;
    float _nextCount;
    PointController _controller;

    public PointController Controller => _controller;

    void Start()
    {
        _sprite = GetComponent<SpriteRenderer>();
        _area = GetComponent<Collider2D>();
        _controller = ControllerFor(holder);
    }

    void Update()
    {
        if (Time.time >= _nextCount)
        {
            _nextCount = Time.time + CountInterval;
            _soldierDifference = CountSoldiers();
        }
        float gradient = Mathf.Clamp(_soldierDifference, -MaxSoldierDifference, MaxSoldierDifference) * Time.deltaTime / 50f;
        holder = Mathf.Clamp(holder + gradient, -1f, 1f);
        UpdateColour();

        PointController controller = ControllerFor(holder);
        if (controller == _controller) return;
        _controller = controller;
        if (controller == PointController.Neutral) return;
        bool player = controller == PointController.Player;
        capturedBy?.Invoke(player);
        PointCaptured?.Invoke(this, player);
        if (Battle.Instance != null) CapturedBy?.Invoke(this, player ? Battle.Instance.player : Battle.Instance.enemy);
    }

    PointController ControllerFor(float value)
    {
        if (value >= triggerThreshold) return PointController.Player;
        if (value <= -triggerThreshold) return PointController.Enemy;
        return PointController.Neutral;
    }

    // Player soldiers inside minus enemy soldiers inside.
    int CountSoldiers()
    {
        Battle battle = Battle.Instance;
        if (battle == null) return 0;
        return Count(battle.player) - Count(battle.enemy);
    }

    int Count(Army army)
    {
        if (army == null) return 0;
        int count = 0;
        foreach (var troop in army.Troops)
        {
            if (troop.IsDefeated) continue;
            foreach (var soldier in troop.Soldiers)
                if (soldier.IsAlive && Contains(troop.WorldPosition(soldier))) count++;
        }
        return count;
    }

    bool Contains(Vector2 point) => _area != null
        ? _area.OverlapPoint(point)
        : ((Vector2)transform.position - point).sqrMagnitude <= _radius * _radius;

    void UpdateColour()
    {
        if (_sprite == null) return;
        _sprite.color = holder > 0
            ? Color.Lerp(NeutralControlled, PlayerControlled, holder)
            : Color.Lerp(NeutralControlled, AIControlled, -holder);
    }
}
