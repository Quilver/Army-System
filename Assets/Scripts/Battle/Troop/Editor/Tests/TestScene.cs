using System.Collections.Generic;
using UnityEngine;

// Clean-up shared by the scenario tests. Edit mode does not call OnDestroy, so destroying a troop's GameObject leaves its
// soldier sprites (scene roots) behind. They would pile up from test to test, shift the next test's hierarchy and with
// it every troop's random stream (Troop.StreamId), so a test's outcome would depend on which tests ran before it.
public static class TestScene
{
    public static void Destroy(List<Object> created)
    {
        TroopObjects.FlushPending();
        foreach (var target in created)
        {
            if (target is not GameObject gameObject || gameObject == null) continue;
            foreach (var troop in gameObject.GetComponentsInChildren<Troop>(true)) troop.DestroyViewsForTests();
        }
        foreach (var target in created)
            if (target != null) Object.DestroyImmediate(target);
        created.Clear();
        TroopObjects.FlushPending();
    }
}
