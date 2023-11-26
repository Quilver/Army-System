using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Map))]
public class MapEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        Map map = (Map)target;
        if (GUILayout.Button("Generate Map"))
        {
            for (int i = map.transform.childCount - 1; i > 0; i--)
            {
                GameObject.DestroyImmediate(map.transform.GetChild(i).gameObject);
            }
            map.Init();
        }
        string mapSize = "Size: " + map.Width + ", " + map.Height;
        GUILayout.Label(mapSize);
        if (GUILayout.Button("Reset Map"))
        {
            for (int i = map.transform.childCount - 1; i > 0; i--)
            {
                Map.Instance = map;
            }
        }

    }
}
