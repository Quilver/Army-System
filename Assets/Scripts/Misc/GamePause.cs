using System;
using System.Collections.Generic;
using UnityEngine;

// One pause shared by everything that stops game time: the battle's deployment phase, the player's pause (P or the
// speed bar) and cutscenes. Each holds the pause under its own key; time runs again only when every holder has
// released it, so one system ending its pause cannot unpause another.
// It also owns the game speed (the speed bar's ½×, 1× and 2×): while nothing holds the pause Time.timeScale is Speed,
// and while paused it is 0. The speed is kept through a pause, so time resumes at the speed it had.
// The battle's end slows the game (Slow, SU-168): while it is slowed Time.timeScale is the slow speed whatever the pause
// holders and Speed say, so the speed bar and the pause key cannot undo the slow-down. Both are still recorded and
// apply again when the slow-down ends (EndSlow).
public static class GamePause
{
    public const float MinSpeed = 0.1f;
    public const float MaxSpeed = 4f;

    static readonly HashSet<object> Holders = new();
    static object _slowHolder;
    static float _slowSpeed = 1f;

    public static bool IsPaused => Holders.Count > 0;

    // Game speed while not paused (1 = normal).
    public static float Speed { get; private set; } = 1f;

    // Raised when time stops (true) or runs again (false).
    public static event Action<bool> Changed;

    // Raised when the game speed changes.
    public static event Action<float> SpeedChanged;

    public static bool IsHeldBy(object holder) => holder != null && Holders.Contains(holder);

    // True while a holder (the battle, as it ends) has slowed the game.
    public static bool IsSlowed => _slowHolder != null;

    // Runs the game at speed (clamped to MinSpeed-MaxSpeed) until holder calls EndSlow, overriding pauses and Speed.
    // A later Slow by another holder takes over.
    public static void Slow(object holder, float speed)
    {
        if (holder == null) return;
        _slowHolder = holder;
        _slowSpeed = Mathf.Clamp(speed, MinSpeed, MaxSpeed);
        Apply(false);
    }

    // Ends holder's slow-down: time goes back to Speed, or stays stopped while something holds the pause.
    public static void EndSlow(object holder)
    {
        if (holder == null || !ReferenceEquals(holder, _slowHolder)) return;
        _slowHolder = null;
        Apply(false);
    }

    public static void Hold(object holder)
    {
        if (holder == null || !Holders.Add(holder)) return;
        Apply(Holders.Count == 1);
    }

    public static void Release(object holder)
    {
        if (holder == null || !Holders.Remove(holder)) return;
        Apply(Holders.Count == 0);
    }

    public static void Set(object holder, bool paused)
    {
        if (paused) Hold(holder);
        else Release(holder);
    }

    // Sets the game speed (clamped to MinSpeed-MaxSpeed). Takes effect at once, or when the pause ends.
    public static void SetSpeed(float speed)
    {
        speed = Mathf.Clamp(speed, MinSpeed, MaxSpeed);
        if (Mathf.Approximately(speed, Speed)) return;
        Speed = speed;
        Apply(false);
        SpeedChanged?.Invoke(Speed);
    }

    static void Apply(bool changed)
    {
        Time.timeScale = _slowHolder != null ? _slowSpeed : Holders.Count > 0 ? 0f : Speed;
        if (changed) Changed?.Invoke(Holders.Count > 0);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Holders.Clear();
        _slowHolder = null;
        _slowSpeed = 1f;
        Changed = null;
        SpeedChanged = null;
        Speed = 1f;
        Time.timeScale = 1f;
    }
}
