using System;
using UnityEngine;
using UnityEngine.Events;

// The battle in this scene: its two armies, the start once deployment is valid, and the end. The battle does not load
// scenes or touch campaign data; BattleFlow (battle-to-campaign bridge) listens to Started and Ended.
public class Battle : MonoBehaviour
{
    public static Battle Instance { get; private set; }

    // Static so the battle flow can listen before any battle scene loads.
    public static event Action<Battle> Started;
    public static event Action<Battle, bool> Ended;

    public event Action Deploy;
    public event Action<bool> BattleOver;

    [SerializeField] UnityEvent _Deploy;
    public Army player, enemy;
    [SerializeField] int invalidDeployments;

    bool _over;
    float _startTime;

    public bool IsOver => _over;
    // Seconds since the battle scene started.
    public float ElapsedTime => Time.time - _startTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ClearStatics()
    {
        Instance = null;
        Started = null;
        Ended = null;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Multiple battle controllers");
            return;
        }
        Instance = this;
    }

    void Start()
    {
        _startTime = Time.time;
        Started?.Invoke(this);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    #region Deployment
    public void StartBattle()
    {
        if (invalidDeployments != 0) return;
        _Deploy?.Invoke();
        Deploy?.Invoke();
    }

    public void UpdateUnitDeployment(bool validDeployment)
    {
        if (validDeployment) invalidDeployments--;
        else invalidDeployments++;
    }
    #endregion

    // Ends the battle once. win is from the player's side.
    public void EndBattle(bool win)
    {
        if (_over) return;
        _over = true;
        BattleOver?.Invoke(win);
        Ended?.Invoke(this, win);
    }

    // Army the troop belongs to: player, enemy or null.
    public Army ArmyOf(Troop troop) => troop != null ? troop.Army : null;
}
