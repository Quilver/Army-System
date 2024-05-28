using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Reinforcements : MonoBehaviour
{
    [SerializeField, Range(1, 120)]
    float firstDeploymentTime;
    [SerializeField]
    bool PlayerReinforcements;
    [SerializeField]
    Vector2 DeployTo;
    [SerializeField]
    GameObject unitsToDeploy;
    Army army;
    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawSphere(transform.position, 0.3f);
        Gizmos.DrawLine(DeployTo, transform.position);
        Gizmos.DrawCube(DeployTo, new(8, 2));
    }
    void Start()
    {
        if (PlayerReinforcements)
            army = Battle.Instance.player;
        else
            army = Battle.Instance.enemy1;
        Invoke("Reinforce", firstDeploymentTime);
        
    }
    void Reinforce()
    {
        if (overlaps == 0)
            Spawn();
        else blah += Spawn;
    }
    void Spawn()
    {
        var unit = GameObject.Instantiate(unitsToDeploy, army.transform);
        unit.transform.position = transform.position;
        army.AddUnit(unit.GetComponent<UnitInterface>());
        Destroy(gameObject);
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
