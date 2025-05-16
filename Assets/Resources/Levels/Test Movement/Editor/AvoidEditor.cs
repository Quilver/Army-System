using MovementSystem;
using MovementSystem.SteeringBehaviour;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

[CustomEditor(typeof(SimpleSteerer))]
public class SteerTestEditor : Editor
{
    
    public void OnSceneGUI()
    {
        var t = target as SimpleSteerer;
        Handles.Label(t.transform.position, $"Velocity is: {t.GetDirection().magnitude}");
        float i = 0;
        foreach (var item in t.GetComponentsInChildren<ISteeringBehaviour>())
        {
            if (!item.enabled) continue;
            i += 1;
            //Handles.Label(t.transform.position+i*Vector3.up, $"{item.GetType().ToString()} steer force is: {item.GetForce()}");
        }
    }
}
