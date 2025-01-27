using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HybridA_starFollow : SteeringBehaviour
{
    [SerializeField]
    Transform targetToFollow;

    public override float priority => throw new System.NotImplementedException();

    public override Vector2 GetDirection()
    {
        throw new System.NotImplementedException();
    }
}
