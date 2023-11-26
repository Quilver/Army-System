using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
[CustomEditor(typeof(Unit))]
public class UnitEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        Unit myScript = (Unit)target;
        if (GUILayout.Button("Generate Models"))
        {
            myScript.create();
        }

    }
}
