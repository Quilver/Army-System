using System.Collections.Generic;
using BattlePrep.Tent;
using Cutscene;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

// SU-187: the tent shows each court member as their scene's primary speaker, the first character who talks with a
// portrait on stage, in place of the placeholder figure.
[Category("Dialogue")]
public sealed class CourtPortraitTests
{
    readonly List<Object> _made = new();

    [TearDown]
    public void TearDown()
    {
        foreach (var o in _made) if (o != null) Object.DestroyImmediate(o);
        _made.Clear();
    }

    Sprite MakeSprite(string name, int width, int height)
    {
        var texture = new Texture2D(width, height);
        var sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        sprite.name = name;
        _made.Add(texture);
        _made.Add(sprite);
        return sprite;
    }

    DialogueScript MakeScript(params DialogueLine[] lines)
    {
        var script = ScriptableObject.CreateInstance<DialogueScript>();
        script.SetLines(new List<DialogueLine>(lines), null);
        _made.Add(script);
        return script;
    }

    [Test]
    public void PrimarySpeakerIsTheFirstToTalkWithAPortrait()
    {
        var laurice = MakeSprite("Laurice", 4, 5);
        var julian = MakeSprite("Julian", 4, 5);
        var script = MakeScript(
            new DialogueLine { Text = "Dawn over the camp." },
            new DialogueLine { Speaker = "Narrator", Text = "No image." },
            new DialogueLine { Speaker = "Laurice", Text = "Do you think this is Claudius' work?", SpeakerSide = DialogueSide.Left,
                               LeftName = "Laurice", LeftPortrait = laurice },
            new DialogueLine { Speaker = "Prince Julian", Text = "Perhaps.", SpeakerSide = DialogueSide.Right,
                               LeftName = "Laurice", LeftPortrait = laurice, RightName = "Prince Julian", RightPortrait = julian });

        Assert.IsTrue(script.TryGetPrimarySpeaker(out var name, out var portrait));
        Assert.AreEqual("Laurice", name);
        Assert.AreSame(laurice, portrait);
    }

    [Test]
    public void ASpeakerOnTheRightUsesTheRightPortrait()
    {
        var jagen = MakeSprite("Jagen", 4, 4);
        var other = MakeSprite("Other", 4, 4);
        var script = MakeScript(new DialogueLine
        {
            Speaker = "Jagen", Text = "My prince.", SpeakerSide = DialogueSide.Right,
            LeftName = "Other", LeftPortrait = other, RightName = "Jagen", RightPortrait = jagen,
        });

        Assert.IsTrue(script.TryGetPrimarySpeaker(out var name, out var portrait));
        Assert.AreEqual("Jagen", name);
        Assert.AreSame(jagen, portrait);
    }

    [Test]
    public void ASceneWithNoPortraitHasNoPrimarySpeaker()
    {
        var script = MakeScript(new DialogueLine { Speaker = "Briefing", Text = "Only narration." });
        Assert.IsFalse(script.TryGetPrimarySpeaker(out var name, out var portrait));
        Assert.IsNull(name);
        Assert.IsNull(portrait);
    }

    [Test]
    public void ThePortraitReplacesTheFigureAtTheSlotsHeightAndCanBeTakenBack()
    {
        var placeholder = MakeSprite("Tent_Captain", 106, 362);
        var portrait = MakeSprite("Laurice", 300, 400);
        var go = new GameObject("Captain", typeof(RectTransform), typeof(Image));
        _made.Add(go);
        var rect = (RectTransform)go.transform;
        rect.sizeDelta = new Vector2(70, 240);
        var image = go.GetComponent<Image>();
        image.sprite = placeholder;
        var hotspot = go.AddComponent<CourtHotspot>();

        hotspot.ShowPortrait(portrait);
        Assert.AreSame(portrait, image.sprite);
        Assert.AreEqual(180f, rect.sizeDelta.x, 0.01f);
        Assert.AreEqual(240f, rect.sizeDelta.y, 0.01f);

        hotspot.ShowPortrait(null);
        Assert.AreSame(placeholder, image.sprite);
        Assert.AreEqual(new Vector2(70, 240), rect.sizeDelta);
    }
}
