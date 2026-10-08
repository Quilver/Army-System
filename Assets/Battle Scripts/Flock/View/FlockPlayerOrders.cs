using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class FlockPlayerOrders : MonoBehaviour
{
    [SerializeField] int _playerTeamId;
    [SerializeField, Min(1f)] float _selectionDragThreshold = 10f;
    [SerializeField, Min(0.1f)] float _clickSelectionRadius = 0.6f;
    [SerializeField, Min(0.1f)] float _clickOrderThreshold = 0.5f;

    readonly List<Flock> _flocks = new();
    readonly List<Flock> _selected = new();
    Camera _camera;
    Vector2? _selectionStart;
    Vector2? _orderStart;
    Vector2 _mousePosition;
    bool _addToSelection;
    float _nextFlockRefresh;

    void Awake()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        _camera = Camera.main;
    }

    void Update()
    {
        if (_camera == null) _camera = Camera.main;
        if (_camera == null) return;
        RefreshFlocks();
        RemoveDestroyedSelections();

        var mouse = Mouse.current;
        if (mouse == null) return;
        _mousePosition = mouse.position.ReadValue();

        if (mouse.leftButton.wasPressedThisFrame && !OverHud(_mousePosition))
        {
            _selectionStart = _mousePosition;
            _addToSelection = IsShiftHeld();
        }
        if (mouse.leftButton.wasReleasedThisFrame && _selectionStart.HasValue)
        {
            FinishSelection(_selectionStart.Value, _mousePosition);
            _selectionStart = null;
        }

        if (mouse.rightButton.wasPressedThisFrame && !OverHud(_mousePosition))
            _orderStart = _mousePosition;
        if (mouse.rightButton.wasReleasedThisFrame && _orderStart.HasValue)
        {
            FinishOrder(ScreenToWorld(_orderStart.Value), ScreenToWorld(_mousePosition));
            _orderStart = null;
        }
    }

    void RefreshFlocks()
    {
        if (Time.unscaledTime < _nextFlockRefresh) return;
        _nextFlockRefresh = Time.unscaledTime + 0.5f;
        _flocks.Clear();
        _flocks.AddRange(FindObjectsByType<Flock>(FindObjectsSortMode.None));
    }

    void FinishSelection(Vector2 start, Vector2 end)
    {
        if ((end - start).sqrMagnitude >= _selectionDragThreshold * _selectionDragThreshold)
        {
            SelectBox(start, end);
            return;
        }

        Flock closest = null;
        float closestDistance = _clickSelectionRadius;
        Vector2 worldPosition = ScreenToWorld(end);
        foreach (var flock in _flocks)
        {
            if (flock == null || flock.TeamId != _playerTeamId) continue;
            float distance = flock.DistanceToNearestBoid(worldPosition);
            if (distance < closestDistance)
            {
                closest = flock;
                closestDistance = distance;
            }
        }

        if (!_addToSelection) ClearSelection();
        if (closest != null) AddSelection(closest);
    }

    void SelectBox(Vector2 start, Vector2 end)
    {
        if (!_addToSelection) ClearSelection();

        Vector2 worldStart = ScreenToWorld(start);
        Vector2 worldEnd = ScreenToWorld(end);
        Rect box = Rect.MinMaxRect(
            Mathf.Min(worldStart.x, worldEnd.x),
            Mathf.Min(worldStart.y, worldEnd.y),
            Mathf.Max(worldStart.x, worldEnd.x),
            Mathf.Max(worldStart.y, worldEnd.y));

        foreach (var flock in _flocks)
        {
            if (flock != null && flock.TeamId == _playerTeamId && flock.HasLivingBoidInRect(box))
                AddSelection(flock);
        }
    }

    void FinishOrder(Vector2 start, Vector2 end)
    {
        if (_selected.Count == 0) return;
        Vector2 drag = end - start;
        if (drag.magnitude < _clickOrderThreshold)
        {
            Flock enemy = EnemyAtPoint(end);
            if (enemy == null)
            {
                OrderAtPoint(end);
                return;
            }

            foreach (Flock flock in _selected)
                if (flock != null) flock.Pursue(enemy);
            return;
        }

        Vector2 tangent = drag.normalized;
        _selected.Sort((a, b) =>
        {
            float aPosition = Vector2.Dot((Vector2)a.transform.position - start, tangent);
            float bPosition = Vector2.Dot((Vector2)b.transform.position - start, tangent);
            int alongLine = aPosition.CompareTo(bPosition);
            return alongLine != 0 ? alongLine : a.transform.position.y.CompareTo(b.transform.position.y);
        });

        float segmentLength = drag.magnitude / _selected.Count;
        for (int i = 0; i < _selected.Count; i++)
        {
            Flock flock = _selected[i];
            Vector2 center = start + tangent * (segmentLength * (i + 0.5f));
            Vector2 enemy = NearestEnemyPosition(flock, center);
            flock.OrderLine(center, tangent, segmentLength * 0.9f, enemy);
        }
    }

    void OrderAtPoint(Vector2 destination)
    {
        Vector2 oldCenter = Vector2.zero;
        foreach (var flock in _selected) oldCenter += (Vector2)flock.transform.position;
        oldCenter /= _selected.Count;

        foreach (var flock in _selected)
        {
            Vector2 offset = (Vector2)flock.transform.position - oldCenter;
            Vector2 center = destination + offset;
            flock.OrderLine(center, flock.LineAxis, flock.LineWidth, NearestEnemyPosition(flock, center));
        }
    }

    Vector2 NearestEnemyPosition(Flock subject, Vector2 from)
    {
        Vector2 nearest = from;
        float nearestDistance = float.PositiveInfinity;
        foreach (var flock in _flocks)
        {
            if (flock == null || flock == subject || flock.TeamId == subject.TeamId) continue;
            float distance = Vector2.SqrMagnitude((Vector2)flock.transform.position - from);
            if (distance >= nearestDistance) continue;
            nearestDistance = distance;
            nearest = flock.transform.position;
        }
        return nearest;
    }

    Flock EnemyAtPoint(Vector2 worldPosition)
    {
        Flock closest = null;
        float closestDistance = float.PositiveInfinity;
        foreach (Flock flock in _flocks)
        {
            if (flock == null || flock.TeamId == _playerTeamId) continue;
            float distance = Mathf.Min(
                flock.DistanceToNearestBoid(worldPosition),
                Vector2.Distance(flock.transform.position, worldPosition));
            float pickRadius = Mathf.Max(_clickSelectionRadius, flock.Radius * 0.12f);
            if (distance >= pickRadius || distance >= closestDistance) continue;
            closest = flock;
            closestDistance = distance;
        }
        return closest;
    }

    void AddSelection(Flock flock)
    {
        if (_selected.Contains(flock)) return;
        _selected.Add(flock);
        flock.SetSelected(true);
    }

    void ClearSelection()
    {
        foreach (var flock in _selected)
            if (flock != null) flock.SetSelected(false);
        _selected.Clear();
    }

    void RemoveDestroyedSelections()
    {
        for (int i = _selected.Count - 1; i >= 0; i--)
        {
            if (_selected[i] != null) continue;
            _selected.RemoveAt(i);
        }
    }

    Vector2 ScreenToWorld(Vector2 screenPosition)
    {
        float cameraDepth = -_camera.transform.position.z;
        Vector3 world = _camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, cameraDepth));
        return world;
    }

    static bool IsShiftHeld()
    {
        var keyboard = Keyboard.current;
        return keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
    }

    static bool OverHud(Vector2 screenPosition)
    {
        float guiY = Screen.height - screenPosition.y;
        return screenPosition.x <= 500f
            && (guiY <= 140f || guiY >= Screen.height - 86f);
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(16f, Screen.height - 86f, 480f, 70f), GUI.skin.box);
        GUILayout.Label("Left-click / drag: select friendly flocks (Shift adds)");
        GUILayout.Label("Right-click / drag: move selection / set battle line   Selected: " + _selected.Count);
        GUILayout.EndArea();

        if (_selectionStart.HasValue && Mouse.current != null)
            DrawSelectionBox(_selectionStart.Value, _mousePosition);
        if (_orderStart.HasValue && Mouse.current != null)
            DrawDragLine(_orderStart.Value, _mousePosition);
    }

    static void DrawSelectionBox(Vector2 start, Vector2 end)
    {
        Rect rect = Rect.MinMaxRect(
            Mathf.Min(start.x, end.x),
            Screen.height - Mathf.Max(start.y, end.y),
            Mathf.Max(start.x, end.x),
            Screen.height - Mathf.Min(start.y, end.y));
        Color previous = GUI.color;
        GUI.color = new Color(0.25f, 0.8f, 1f, 0.16f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = new Color(0.45f, 0.88f, 1f, 0.9f);
        const float border = 2f;
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, rect.width, border), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMax - border, rect.width, border), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, border, rect.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMax - border, rect.yMin, border, rect.height), Texture2D.whiteTexture);
        GUI.color = previous;
    }

    static void DrawDragLine(Vector2 start, Vector2 end)
    {
        Vector2 guiStart = new(start.x, Screen.height - start.y);
        Vector2 guiEnd = new(end.x, Screen.height - end.y);
        Vector2 delta = guiEnd - guiStart;
        if (delta.sqrMagnitude < 1f) return;

        Matrix4x4 previous = GUI.matrix;
        GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, guiStart);
        GUI.DrawTexture(new Rect(guiStart.x, guiStart.y - 1f, delta.magnitude, 2f), Texture2D.whiteTexture);
        GUI.matrix = previous;
    }
}
