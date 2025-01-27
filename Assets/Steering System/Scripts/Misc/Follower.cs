using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Follower : MonoBehaviour
{

    [SerializeField]
    Transform follow;
    // Update is called once per frame
    void Update()
    {
        if (follow == null)return;
        Vector2 postion = transform.position;
        Vector2 following = follow.position;
        transform.position = new Vector3(following.x, following.y, transform.position.z);   
        
    }
}
