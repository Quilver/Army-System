using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Flock))]
public sealed class FlockEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var flock = (Flock)target;
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.FloatField("Shock", flock.Shock);
    }
}
