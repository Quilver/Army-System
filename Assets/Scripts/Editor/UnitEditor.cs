using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
[CustomEditor(typeof(UnitR))]
public class UnitEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        UnitR myScript = (UnitR)target;
        if (GUILayout.Button("Generate Models"))
        {
            //myScript.unitMovementHandler.SetRotation();
            //myScript.UpdateVisual();
        }

    }
}
