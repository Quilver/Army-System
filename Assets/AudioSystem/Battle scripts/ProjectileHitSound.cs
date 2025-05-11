using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileHitSound : MonoBehaviour
{
    [SerializeField]
    GameObject hitSoundPrefab;
    private void OnDestroy()
    {
        var sound = Instantiate(hitSoundPrefab, transform.parent);
        sound.transform.position=transform.position;
    }
}
