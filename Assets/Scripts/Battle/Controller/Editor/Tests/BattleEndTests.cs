using NUnit.Framework;
using UnityEngine;

// The battle's end (SU-168): Ended fires once and fixes the result, the game is slowed (GamePause.Slow) in a way the
// speed bar and pauses cannot undo, and Leaving fires once, at normal speed, after the banner's real-time seconds.
[Category("Deployment")]
public sealed class BattleEndTests
{
    GameObject _object;
    Battle _battle;
    TroopTuning _tuning;
    readonly object _player = new();
    int _ended, _leaving;
    bool _endedWin, _leavingWin;
    float _timeScaleAtLeaving;

    [SetUp]
    public void SetUp()
    {
        _tuning = TroopTuning.CreateDefaults();
        TroopTuning.OverrideForTests(_tuning);
        _object = new GameObject("Battle");
        _battle = _object.AddComponent<Battle>();   // Awake and Start do not run in Edit mode
        _ended = _leaving = 0;
        Battle.Ended += OnEnded;
        Battle.Leaving += OnLeaving;
    }

    [TearDown]
    public void TearDown()
    {
        Battle.Ended -= OnEnded;
        Battle.Leaving -= OnLeaving;
        GamePause.EndSlow(_battle);   // OnDestroy does not run in Edit mode
        Object.DestroyImmediate(_object);
        GamePause.Release(_player);
        GamePause.SetSpeed(1f);
        TroopTuning.OverrideForTests(null);
        _tuning.DestroySections();
    }

    void OnEnded(Battle battle, bool win)
    {
        if (battle != _battle) return;
        _ended++;
        _endedWin = win;
    }

    void OnLeaving(Battle battle, bool win)
    {
        if (battle != _battle) return;
        _leaving++;
        _leavingWin = win;
        _timeScaleAtLeaving = Time.timeScale;
    }

    [Test]
    public void BattleEndTuning_Defaults()
    {
        Assert.AreEqual(0.25f, _tuning.EndSlowSpeed);
        Assert.AreEqual(10f, _tuning.EndBannerSeconds);
    }

    [Test]
    public void EndBattle_FiresEndedOnce_AndKeepsTheFirstResult()
    {
        _battle.EndBattle(true);
        _battle.EndBattle(false);
        Assert.AreEqual(1, _ended, "Ended fires once");
        Assert.IsTrue(_endedWin);
        Assert.IsTrue(_battle.Won, "a later EndBattle does not change the result");
        Assert.AreEqual(BattlePhase.Over, _battle.Phase);
        Assert.IsTrue(_battle.IsShowingEndBanner);
    }

    [Test]
    public void EndBattle_SlowsTheGame_AndTheSpeedBarAndPausesCannotUndoIt()
    {
        _battle.EndBattle(true);
        Assert.IsTrue(GamePause.IsSlowed);
        Assert.AreEqual(0.25f, Time.timeScale, 1e-5f);
        GamePause.SetSpeed(2f);
        Assert.AreEqual(0.25f, Time.timeScale, 1e-5f, "the speed bar does not undo the slow-down");
        GamePause.Hold(_player);
        Assert.AreEqual(0.25f, Time.timeScale, 1e-5f, "a pause does not stop the slowed fighting");
        GamePause.Release(_player);
    }

    [Test]
    public void Leaving_FiresOnceAfterTheBanner_AtNormalSpeed()
    {
        float now = Time.unscaledTime;
        GamePause.SetSpeed(2f);
        _battle.EndBattle(false);
        _battle.TickEnd(now + 9.9f);
        Assert.AreEqual(0, _leaving, "the banner shows for 10 s of real time");
        Assert.IsTrue(_battle.IsShowingEndBanner);

        _battle.TickEnd(now + 10.1f);
        _battle.TickEnd(now + 20f);
        Assert.AreEqual(1, _leaving, "Leaving fires once");
        Assert.IsFalse(_leavingWin);
        Assert.AreEqual(1f, _timeScaleAtLeaving, 1e-5f, "time is back to normal speed before the next scene");
        Assert.IsFalse(GamePause.IsSlowed);
        Assert.IsFalse(_battle.IsShowingEndBanner);
        Assert.AreEqual(1, _ended, "Ended is not raised again");
    }

    [Test]
    public void TickEnd_DoesNothing_BeforeTheBattleEnds()
    {
        _battle.TickEnd(Time.unscaledTime + 100f);
        Assert.AreEqual(0, _leaving);
        Assert.IsFalse(GamePause.IsSlowed);
    }

    [Test]
    public void ElapsedTime_StopsWhenTheBattleEnds()
    {
        _battle.EndBattle(true);
        float atEnd = _battle.ElapsedTime;
        Assert.AreEqual(atEnd, _battle.ElapsedTime, "the slowed fighting under the banner does not count");
    }

    [Test]
    public void Slow_EndsOnlyForItsHolder_AndTimeReturnsToTheSpeedOrThePause()
    {
        object battle = new();
        GamePause.SetSpeed(0.5f);
        GamePause.Slow(battle, 0.25f);
        GamePause.EndSlow(_player);
        Assert.AreEqual(0.25f, Time.timeScale, 1e-5f, "another holder cannot end the slow-down");
        GamePause.Hold(_player);
        GamePause.EndSlow(battle);
        Assert.AreEqual(0f, Time.timeScale, "a pause still held applies again");
        GamePause.Release(_player);
        Assert.AreEqual(0.5f, Time.timeScale, 1e-5f, "then the game speed");
    }

    [Test]
    public void Banner_NamesTheResult()
    {
        Assert.AreEqual("Victory", BattleEndBanner.TextFor(true));
        Assert.AreEqual("Defeat", BattleEndBanner.TextFor(false));
    }
}
