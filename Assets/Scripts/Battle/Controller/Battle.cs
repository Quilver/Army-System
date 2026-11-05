using System;
using UnityEngine;
using UnityEngine.Events;

public enum BattlePhase { Deployment, Fighting, Over }

// The battle in this scene: its two armies, the optional deployment phase, the start of the fighting and the end. The
// battle does not load scenes or touch campaign data; BattleFlow (battle-to-campaign bridge) listens to Started and
// Ended.
// Deployment: while it lasts the battle simulation is paused (GamePause) and the player places troops inside the
// deployment zone (DeploymentZone); Start Battle (StartBattle) ends it. The enemy AI runs from the scene start and acts
// once time runs. A battle without deployment, or without a deployment zone for the player, starts fighting at once.
public class Battle : MonoBehaviour
{
    public static Battle Instance { get; private set; }

    // Static so the battle flow can listen before any battle scene loads.
    public static event Action<Battle> Started;
    public static event Action<Battle, bool> Ended;

    // The fighting begins: deployment ended, or at the start when there is no deployment.
    public event Action Deploy;
    public event Action<BattlePhase> PhaseChanged;
    public event Action<bool> BattleOver;

    [SerializeField, Tooltip("The player places troops in the deployment zone, with the battle paused, before it starts.")]
    bool _deployment = true;
    [SerializeField, Tooltip("Called when the fighting begins (Start Battle, or at once without deployment).")]
    UnityEvent _Deploy;
    public Army player, enemy;

    bool _over;
    float _startTime;

    public bool IsOver => _over;
    public bool HasDeploymentPhase => _deployment;
    public BattlePhase Phase { get; private set; } = BattlePhase.Fighting;
    public bool IsDeploying => Phase == BattlePhase.Deployment;
    // Seconds of battle time since the scene started. Time stands still while deploying, so it is fighting time.
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
        // Deploy from the first frame, paused, so nothing moves before Start. The deployment zone registers itself
        // first (it runs earlier, DefaultExecutionOrder).
        if (_deployment && DeploymentZone.For(player) != null)
        {
            Phase = BattlePhase.Deployment;
            GamePause.Hold(this);
        }
    }

    void Start()
    {
        _startTime = Time.time;
        Started?.Invoke(this);
        if (Phase == BattlePhase.Deployment) SetPhase(BattlePhase.Deployment);
        else BeginFighting();
    }

    void OnDestroy()
    {
        GamePause.Release(this);
        if (Instance == this) Instance = null;
    }

    #region Deployment
    // Ends the deployment phase and starts the fighting (the Start Battle button).
    public void StartBattle()
    {
        if (Phase != BattlePhase.Deployment) return;
        BeginFighting();
    }

    void BeginFighting()
    {
        GamePause.Release(this);
        SetPhase(BattlePhase.Fighting);
        _Deploy?.Invoke();
        Deploy?.Invoke();
    }

    void SetPhase(BattlePhase phase)
    {
        Phase = phase;
        PhaseChanged?.Invoke(phase);
    }
    #endregion

    // Ends the battle once. win is from the player's side.
    public void EndBattle(bool win)
    {
        if (_over) return;
        _over = true;
        GamePause.Release(this);
        SetPhase(BattlePhase.Over);
        BattleOver?.Invoke(win);
        Ended?.Invoke(this, win);
    }

    // Army the troop belongs to: player, enemy or null.
    public Army ArmyOf(Troop troop) => troop != null ? troop.Army : null;
}
