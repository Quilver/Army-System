using System;
using UnityEngine;
using UnityEngine.Events;

public enum BattlePhase { Deployment, Fighting, Over }

// The battle in this scene: its two armies, the optional deployment phase, the start of the fighting and the end. The
// battle does not load scenes or touch campaign data; BattleFlow (battle-to-campaign bridge) listens to Started, Ended
// and Leaving.
// Deployment: while it lasts the battle simulation is paused (GamePause) and the player places troops inside the
// deployment zone (DeploymentZone); Start Battle (StartBattle) ends it. The enemy AI runs from the scene start and acts
// once time runs. A battle without deployment, or without a deployment zone for the player, starts fighting at once.
// End (SU-168): EndBattle ends the battle once. BattleOver and Ended fire at that moment, and the result is fixed then
// (ElapsedTime stops; Ended's listeners record what they need). The fighting goes on slowed to EndSlowSpeed (GamePause.Slow,
// which the speed bar and the pause key cannot undo) while a Victory or Defeat banner shows (BattleEndBanner) for
// EndBannerSeconds of real time (Battle End Tuning). Then the game speed is back to normal and Leaving fires, once:
// BattleFlow loads the next scene on it. Player orders are ignored once the battle is over (TroopPlayerOrders).
public class Battle : MonoBehaviour
{
    public static Battle Instance { get; private set; }

    // Static so the battle flow can listen before any battle scene loads.
    public static event Action<Battle> Started;
    public static event Action<Battle, bool> Ended;
    // Raised once, after the end banner, when the battle scene should be left (SU-168). win is from the player's side.
    public static event Action<Battle, bool> Leaving;

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
    bool _left;
    bool _won;
    float _startTime;
    float _endTime;
    float _leaveAt;      // Time.unscaledTime at which the end banner closes
    readonly BattleEndBanner _banner = new();

    public bool IsOver => _over;
    // The player won (valid once IsOver).
    public bool Won => _won;
    // The battle is over and its end banner is up (before Leaving).
    public bool IsShowingEndBanner => _over && !_left;
    public bool HasLeft => _left;
    public bool HasDeploymentPhase => _deployment;
    public BattlePhase Phase { get; private set; } = BattlePhase.Fighting;
    public bool IsDeploying => Phase == BattlePhase.Deployment;
    // Seconds of battle time since the scene started. Time stands still while deploying, so it is fighting time. It stops
    // when the battle ends, so the slowed fighting under the end banner does not count (SU-168).
    public float ElapsedTime => (_over ? _endTime : Time.time) - _startTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ClearStatics()
    {
        Instance = null;
        Started = null;
        Ended = null;
        Leaving = null;
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
        GamePause.EndSlow(this);
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

    // Ends the battle once (later calls are ignored). win is from the player's side. The result is fixed now; the banner
    // and the slowed fighting follow (SU-168).
    public void EndBattle(bool win)
    {
        if (_over) return;
        _over = true;
        _won = win;
        _endTime = Time.time;
        GamePause.Release(this);
        TroopTuning tuning = TroopTuning.Active;
        GamePause.Slow(this, tuning.EndSlowSpeed);
        _leaveAt = Time.unscaledTime + tuning.EndBannerSeconds;
        SetPhase(BattlePhase.Over);
        BattleOver?.Invoke(win);
        Ended?.Invoke(this, win);
    }

    void Update() => TickEnd(Time.unscaledTime);

    // Closes the end banner once its time (unscaled, real seconds) is up: the game speed goes back to normal, then
    // Leaving fires, once. Internal for tests.
    internal void TickEnd(float unscaledNow)
    {
        if (!_over || _left || unscaledNow < _leaveAt) return;
        _left = true;
        GamePause.EndSlow(this);
        GamePause.SetSpeed(1f);   // the next scene runs at normal speed
        Leaving?.Invoke(this, _won);
    }

    void OnGUI()
    {
        if (!IsShowingEndBanner) return;
        GUI.depth = BattleEndBanner.GuiDepth;
        _banner.Draw(_won);
    }

    // Army the troop belongs to: player, enemy or null.
    public Army ArmyOf(Troop troop) => troop != null ? troop.Army : null;
}
