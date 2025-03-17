using System.Collections;
using System.Collections.Generic;
using SoftBody;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    // Start is called before the first frame update
    Rigidbody2D body;
    Collider2D col;
    void Start()
    {
        body = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }
    [SerializeField]
    float speed;
    void Update()
    {
        if(!col.enabled && !Physics2D.OverlapCircle(transform.position, 0.5f)) 
            col.enabled = true;
        else
            return;
        speed=body.velocity.magnitude;
        if (speed < 1) Destroy(gameObject);
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        var unit = GetComponent<Model>();
        if (unit == null) return;
        unit.Hit(speed * body.mass);
    }
}
