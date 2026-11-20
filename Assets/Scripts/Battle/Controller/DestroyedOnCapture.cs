using UnityEngine;

// A capture point that is destroyed when the given army takes it (SU-184). Mission 2's secondary objective: the enemy's
// bandit captures the village and destroys it, so the player can no longer take it for its gold. Once destroyed the point
// keeps its owner and never changes hands again (CapturePoint.DestroyPoint).
[DisallowMultipleComponent, RequireComponent(typeof(CapturePoint))]
public class DestroyedOnCapture : MonoBehaviour
{
    [SerializeField, Tooltip("The army whose capture destroys the point.")]
    CapturePoint.PointController _destroyedBy = CapturePoint.PointController.Enemy;

    CapturePoint _point;

    public CapturePoint.PointController DestroyedBy => _destroyedBy;

    CapturePoint Point
    {
        get
        {
            if (_point == null) _point = GetComponent<CapturePoint>();
            return _point;
        }
    }

    void OnEnable()
    {
        if (Point != null) Point.capturedBy += HandleCaptured;
    }

    void OnDisable()
    {
        if (_point != null) _point.capturedBy -= HandleCaptured;
    }

    // Called when the point changes hands (true = the player took it). Destroys it if the taker is DestroyedBy.
    public void HandleCaptured(bool playerCaptured)
    {
        var taker = playerCaptured ? CapturePoint.PointController.Player : CapturePoint.PointController.Enemy;
        if (taker != _destroyedBy || Point == null || Point.IsDestroyed) return;
        Point.DestroyPoint();
        Debug.Log($"{name} was destroyed by the {(playerCaptured ? "player" : "enemy")}.");
    }
}
