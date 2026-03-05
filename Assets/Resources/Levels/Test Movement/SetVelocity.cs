using System;
using UnityEngine;

public class SetVelocity : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField, Range(0, 20)] float speed;
    [SerializeField, Range(0, 4)] float mass = 1;
    [SerializeField, Range(0.1f, 5)] float timeMaxSpeed = 1;
    void Start()
    {
        float mass = GetComponentInParent<Rigidbody2D>().mass;
        float force = mass * speed / timeMaxSpeed;
        GetComponent<FrictionJoint2D>().maxForce = force * this.mass;
        GetComponent<Rigidbody2D>().linearVelocity = Vector2.up * speed;
        Invoke("Show", 1);
    }
    void Show()
    {
        Debug.Log("speed of " + transform.parent.name + " is at " + GetComponent<Rigidbody2D>().linearVelocity.magnitude);
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
