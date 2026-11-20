using NUnit.Framework;
using UnityEngine;

// Destroyed capture points (SU-184): DestroyedOnCapture destroys its point when the given army takes it, and a destroyed
// point is no longer contested, is drawn in the destroyed colour and is destroyed only once.
[Category("Battle")]
public sealed class CapturePointDestroyTests
{
    GameObject _go;

    [TearDown]
    public void TearDown()
    {
        if (_go != null) Object.DestroyImmediate(_go);
    }

    [Test]
    public void EnemyCapture_DestroysThePoint_PlayerCaptureDoesNot()
    {
        _go = new GameObject("Village");
        var destroyer = _go.AddComponent<DestroyedOnCapture>();
        var point = _go.GetComponent<CapturePoint>();
        Assert.IsNotNull(point, "the capture point comes with it");
        Assert.AreEqual(CapturePoint.PointController.Enemy, destroyer.DestroyedBy, "destroyed by the enemy by default");
        int raised = 0;
        point.destroyed += () => raised++;

        destroyer.HandleCaptured(playerCaptured: true);
        Assert.IsFalse(point.IsDestroyed, "the player taking it does not destroy it");

        destroyer.HandleCaptured(playerCaptured: false);
        Assert.IsTrue(point.IsDestroyed, "the enemy taking it destroys it");
        Assert.IsFalse(point.IsContested, "a destroyed point is not contested");
        Assert.AreEqual(1, raised);
    }

    [Test]
    public void DestroyPoint_UsesTheDestroyedColour_AndHappensOnce()
    {
        _go = new GameObject("Village", typeof(SpriteRenderer));
        var point = _go.AddComponent<CapturePoint>();
        var sprite = _go.GetComponent<SpriteRenderer>();
        sprite.color = Color.white;
        int raised = 0;
        point.destroyed += () => raised++;

        point.DestroyPoint();
        point.DestroyPoint();

        Assert.IsTrue(point.IsDestroyed);
        Assert.AreEqual(1, raised, "destroyed once");
        Assert.Less(sprite.color.r + sprite.color.g + sprite.color.b, 1.5f, "drawn dark");
    }
}
