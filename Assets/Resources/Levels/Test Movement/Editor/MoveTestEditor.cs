using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

[CustomEditor(typeof(MoveTest))]
public class MoveTestEditor : Editor
{
    
    public void OnSceneGUI()
    {
        var t = target as MoveTest;
        if (t.units == null) return;
        for (int i = 0; i < t.units.Length; i++)
        {
            Handles.Label(t.units[i].TargetPosition, $"Velocity is: {t.bodies[i].velocity.magnitude}");
        }
    }
}
