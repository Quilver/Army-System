using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Reinforcements : MonoBehaviour
{
    [SerializeField]
    Vector2 DeployTo;
    [SerializeField]
    GameObject unitsToDeploy;
    [SerializeField]
    ArmyData army;
    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawSphere(transform.position, 0.3f);
        Gizmos.DrawLine(DeployTo, transform.position);
        Gizmos.DrawCube(DeployTo, new(8, 2));
    }
    bool CanReinforce
    {
        get => overlaps == 0;
    }
    public void Spawn()
    {
        throw new NotImplementedException();
    }
    [SerializeField]
    int overlaps = 0;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == 8) return;
        overlaps++;
    }
    Action blah;
    private void OnTriggerExit2D(Collider2D collision)
    {
        overlaps--;
        if(blah != null && overlaps == 0)
            Spawn();
    }
}
