using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowMouse : MonoBehaviour
{
    
    void Update()
    {
        if (Input.GetMouseButton(1))
            transform.position = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }
}
