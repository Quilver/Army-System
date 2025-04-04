using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Deploy : MonoBehaviour
{
    [SerializeField]
    List<GameObject> deploymentZones;
    public void StartBattle()
    {
        foreach (var deploymentZone in deploymentZones) Destroy(deploymentZone);    
        Notifications.Deployed(null);
        Destroy(gameObject);
    }
}
