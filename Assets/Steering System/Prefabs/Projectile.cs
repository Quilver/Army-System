using System.Collections;
using System.Collections.Generic;
using SoftBody;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    // Start is called before the first frame update
    Rigidbody2D body;
    Collider2D col;
    public void Setup(Vector2 direction, float force)
    {
        body = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        body.AddForce(direction.normalized * force);
        float desiredAngle = Vector2.SignedAngle(Vector2.up, direction);
        transform.rotation = Quaternion.Euler(0, 0, desiredAngle);
    }
    [SerializeField]
    float speed;
    void Update()
    {
        if (!col.enabled && !Physics2D.OverlapCircle(transform.position, 0.5f)) 
            col.enabled = true;
        else if(!col.enabled) return;
        speed=body.velocity.magnitude;
        if (speed < 5) Destroy(gameObject);
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        var unit = collision.gameObject.GetComponent<Model>();
        if (unit == null) return;
        unit.Hit(Random.Range(speed * body.mass /2, speed * body.mass));
        Destroy(gameObject);
    }
}
