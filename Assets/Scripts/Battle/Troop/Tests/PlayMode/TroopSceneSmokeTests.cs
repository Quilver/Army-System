#if UNITY_EDITOR
using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

// Verification gate V6 in real Play mode: the Troop test scene runs for two minutes of game time without any error or
// exception in the Console (the Test Framework fails the test on any logged error). Frames are fixed at 1/30 s, so the
// run takes as long as the machine needs, not two real minutes.
// This assembly cannot reference Assembly-CSharp, so troops are counted through their type name only.
public sealed class TroopSceneSmokeTests
{
    const string Level1TroopTest = "Assets/Resources/Levels/Test/Level 1 Troop Test.unity";
    const float GameSeconds = 120f;

    [UnityTest]
    [Timeout(600000)]
    public IEnumerator Level1TroopTest_RunsTwoMinutesWithoutErrors()
    {
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Level1TroopTest, new LoadSceneParameters(LoadSceneMode.Single));
        Type troopType = Type.GetType("Troop, Assembly-CSharp");
        Assert.IsNotNull(troopType, "Troop type");
        int initialTroops = Object.FindObjectsByType(troopType, FindObjectsSortMode.None).Length;
        Assert.Greater(initialTroops, 0, "The scene has troops");

        Time.captureDeltaTime = 1f / 30f;
        try
        {
            float start = Time.time;
            while (Time.time - start < GameSeconds)
                yield return null;
            int remaining = Object.FindObjectsByType(troopType, FindObjectsSortMode.None).Length;
            Debug.Log($"[TroopSmoke] {initialTroops} troops at start, {remaining} after {GameSeconds:0} s.");
        }
        finally
        {
            Time.captureDeltaTime = 0f;
        }
    }
}
#endif
