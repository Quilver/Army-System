using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnTowards : MonoBehaviour
{
    Rigidbody2D _body;
    Rigidbody2D Body
    {
        get
        {
            if (_body == null) _body = GetComponentInParent<Rigidbody2D>();
            return _body;
        }
    }
    private void FixedUpdate()
    {

        Body.transform.up = Vector3.MoveTowards(Body.transform.up, Body.linearVelocity.normalized, Time.deltaTime).normalized;
    }
}
