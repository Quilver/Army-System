using NUnit.Framework;
using UnityEngine;

// The battle speed bar: the game speed kept by GamePause, and the Pause / ½ / Play / 2× buttons on TroopPlayerOrders.
[Category("Deployment")]
public sealed class BattleSpeedTests
{
    GameObject _object;
    TroopPlayerOrders _orders;
    readonly object _cutscene = new();

    [SetUp]
    public void SetUp()
    {
        _object = new GameObject("Player Orders");
        _orders = _object.AddComponent<TroopPlayerOrders>();
    }

    [TearDown]
    public void TearDown()
    {
        GamePause.Release(_orders);
        GamePause.Release(_cutscene);
        GamePause.SetSpeed(1f);
        Object.DestroyImmediate(_object);
    }

    [Test]
    public void Speed_IsTheTimeScaleWhileNotPaused_AndIsKeptThroughAPause()
    {
        GamePause.SetSpeed(2f);
        Assert.AreEqual(2f, Time.timeScale);
        GamePause.Hold(_cutscene);
        Assert.AreEqual(0f, Time.timeScale);
        GamePause.SetSpeed(0.5f);
        Assert.AreEqual(0f, Time.timeScale, "a speed change does not end a pause");
        GamePause.Release(_cutscene);
        Assert.AreEqual(0.5f, Time.timeScale, "time resumes at the speed it was set to");
    }

    [Test]
    public void Speed_IsClamped()
    {
        GamePause.SetSpeed(100f);
        Assert.AreEqual(GamePause.MaxSpeed, GamePause.Speed);
        GamePause.SetSpeed(0f);
        Assert.AreEqual(GamePause.MinSpeed, GamePause.Speed);
    }

    [Test]
    public void SpeedButtons_PauseKeepsTheSpeed_OthersSetItAndResume()
    {
        _orders.PressSpeedButton(BattleSpeedButton.Double);
        Assert.IsFalse(_orders.IsPaused);
        Assert.AreEqual(2f, Time.timeScale);

        _orders.PressSpeedButton(BattleSpeedButton.Pause);
        Assert.IsTrue(_orders.IsPaused);
        Assert.AreEqual(0f, Time.timeScale);
        Assert.AreEqual(2f, GamePause.Speed);

        _orders.PressSpeedButton(BattleSpeedButton.Half);
        Assert.IsFalse(_orders.IsPaused);
        Assert.AreEqual(0.5f, Time.timeScale);

        _orders.PressSpeedButton(BattleSpeedButton.Pause);
        _orders.PressSpeedButton(BattleSpeedButton.Play);
        Assert.IsFalse(_orders.IsPaused);
        Assert.AreEqual(1f, Time.timeScale);
    }

    [Test]
    public void SpeedButtons_DoNotEndAnotherSystemsPause()
    {
        GamePause.Hold(_cutscene);
        _orders.PressSpeedButton(BattleSpeedButton.Double);
        Assert.AreEqual(0f, Time.timeScale, "a cutscene's pause holds");
        GamePause.Release(_cutscene);
        Assert.AreEqual(2f, Time.timeScale);
    }

    [TestCase(true, 2f, BattleSpeedButton.Pause)]
    [TestCase(false, 0.5f, BattleSpeedButton.Half)]
    [TestCase(false, 1f, BattleSpeedButton.Play)]
    [TestCase(false, 2f, BattleSpeedButton.Double)]
    public void ActiveButton_MatchesTheState(bool paused, float speed, BattleSpeedButton expected)
    {
        Assert.AreEqual(expected, BattleSpeedHud.ActiveButton(paused, speed));
    }
}
