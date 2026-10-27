using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Command-line smoke test (Tools/UnityCli "smoke"): opens a Troop test scene, plays it in real Play mode for a number of
// seconds of game time (fixed 1/30 s frames, so it runs as fast as the machine allows) and exits with code 1 if any
// error, assert or exception was logged while playing (verification gate V6).
//   Unity.exe -batchmode -projectPath <project> -executeMethod TroopCliSmoke.Run [-smokeSeconds 120] [-smokeScene <path>]
// Entering Play mode reloads the script domain, so the run state lives in SessionState and the handlers are re-attached
// by the static constructor ([InitializeOnLoad]) after each reload.
[InitializeOnLoad]
public static class TroopCliSmoke
{
    const string DefaultScene = "Assets/Resources/Levels/Test/Level 1 Troop Test.unity";
    const float FrameTime = 1f / 30f;
    const string ActiveKey = "TroopCliSmoke.Active";
    const string SecondsKey = "TroopCliSmoke.Seconds";
    const string ErrorsKey = "TroopCliSmoke.Errors";
    const string ExitingKey = "TroopCliSmoke.Exiting";
    const string RealDeadlineKey = "TroopCliSmoke.RealDeadline";

    static bool _inCallback;
    static float _nextReport;

    static TroopCliSmoke()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        Attach();
    }

    public static void Run()
    {
        float seconds = ArgFloat("-smokeSeconds", 120f);
        string scene = ArgString("-smokeScene", DefaultScene);
        SessionState.SetBool(ActiveKey, true);
        SessionState.SetFloat(SecondsKey, seconds);
        SessionState.SetInt(ErrorsKey, 0);
        SessionState.SetBool(ExitingKey, false);
        // Safety net: give up after this much real time even if Play mode never starts.
        SessionState.SetFloat(RealDeadlineKey, (float)EditorApplication.timeSinceStartup + 600f);
        Debug.Log($"[TroopSmoke] Opening {scene}, playing {seconds:0} s of game time.");
        EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);
        Attach();
        EditorApplication.EnterPlaymode();
    }

    static void Attach()
    {
        Application.logMessageReceived -= OnLog;
        Application.logMessageReceived += OnLog;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    static void Tick()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        if (EditorApplication.timeSinceStartup > SessionState.GetFloat(RealDeadlineKey, float.MaxValue))
        {
            Debug.Log("[TroopSmoke] Timed out waiting for Play mode.");
            Finish(1);
            return;
        }
        if (!EditorApplication.isPlaying || SessionState.GetBool(ExitingKey, false)) return;

        Time.captureDeltaTime = FrameTime;
        if (Time.time >= _nextReport)
        {
            _nextReport = Time.time + 20f;
            Report("t=" + Time.time.ToString("0"));
        }
        if (Time.time < SessionState.GetFloat(SecondsKey, 120f)) return;
        SessionState.SetBool(ExitingKey, true);
        Report("end");
        Time.captureDeltaTime = 0f;
        EditorApplication.ExitPlaymode();
    }

    static void OnPlayModeChanged(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(ExitingKey, false))
            Finish(SessionState.GetInt(ErrorsKey, 0) > 0 ? 1 : 0);
    }

    static void Finish(int code)
    {
        int errors = SessionState.GetInt(ErrorsKey, 0);
        SessionState.EraseBool(ActiveKey);
        Application.logMessageReceived -= OnLog;
        EditorApplication.update -= Tick;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        Debug.Log($"[TroopSmoke] Finished: {errors} error(s), exit code {code}.");
        EditorApplication.Exit(code);
    }

    static void Report(string label)
    {
        var troops = UnityEngine.Object.FindObjectsByType<Troop>(FindObjectsSortMode.None);
        int soldiers = 0;
        int routed = 0;
        int engaged = 0;
        int chained = 0;
        int charges = 0;
        int clashes = 0;
        foreach (var troop in troops)
        {
            soldiers += troop.LivingSoldierCount;
            if (troop.IsRouted) routed++;
            if (troop.IsEngaged) engaged++;
            if (troop.IsChained) chained++;
            var summary = troop.MeleeMetrics.Snapshot(troop.LivingSoldierCount);
            charges += summary.Charges;
            clashes += summary.Clashes;
        }
        Debug.Log($"[TroopSmoke] {label}: {troops.Length} troops, {soldiers} soldiers, {engaged} engaged, {chained} chained, " +
                  $"{routed} routed, last 30 s: {charges} charges, {clashes} clash records.");
    }

    static void OnLog(string message, string stackTrace, LogType type)
    {
        if (_inCallback || !EditorApplication.isPlaying) return;
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        _inCallback = true;
        SessionState.SetInt(ErrorsKey, SessionState.GetInt(ErrorsKey, 0) + 1);
        Debug.Log($"[TroopSmoke] {type}: {message}\n{stackTrace}");
        _inCallback = false;
    }

    static float ArgFloat(string name, float fallback)
    {
        string value = ArgString(name, null);
        return value != null && float.TryParse(value, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out float parsed) ? parsed : fallback;
    }

    static string ArgString(string name, string fallback)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i + 1 < args.Length; i++)
            if (args[i] == name) return args[i + 1];
        return fallback;
    }
}
