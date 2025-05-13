using RangedWeapons;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AudioSystem
{
    public class ProjectileHitSound : MonoBehaviour
    {
        private void Start()
        {
            GetComponent<IProjectile>().Hit += HitSound;
        }
        private void HitSound()
        {
            GetComponent<AudioSource>().Play();
            //var sound = Instantiate(hitSoundPrefab, transform.parent);
            //sound.transform.position=transform.position;

        }
    }
}

