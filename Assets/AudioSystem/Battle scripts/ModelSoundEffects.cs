using ModelComponents;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AudioSystem
{
    public class ModelSoundEffects : MonoBehaviour
    {
        [SerializeField]
        AudioClip[] _attackSounds, _shootSounds, _deathGruntSounds;
        [SerializeField]
        AudioSource _marchingSource, _attackSource, _shootSource, _deathGruntSource;

        Rigidbody2D _body;
        private void Start()
        {
            _body = GetComponentInParent<Rigidbody2D>();
            transform.parent.GetComponentInChildren<IMeleeAttack>().Strike += StrikeSound;
            transform.parent.GetComponentInChildren<Shooter>().Shot += ShootSound;
            transform.parent.GetComponentInParent<IDeath>().Dead.AddListener(DeathSound);
        }

        void Update()
        {
            MarchSound();
        }
        
        void MarchSound()
        {
            if (_body.velocity.magnitude > 0.1f)
            {
                _marchingSource.volume = Mathf.Clamp(_body.velocity.magnitude, 0.1f, 0.9f);
                _marchingSource.pitch = Mathf.Lerp(0.8f, 1.1f, _body.velocity.magnitude / 2);
                if (_marchingSource.enabled) return;
                _marchingSource.enabled = true;
                _marchingSource.Play();
            }
            else
                _marchingSource.enabled = false;
        }
        void StrikeSound()
        {
            _attackSource.clip = _attackSounds[Random.Range(0, _attackSounds.Length)];
            _attackSource.pitch = Random.Range(0.8f, 1.2f);
            _attackSource.Play();
        }
        void ShootSound()
        {
            _shootSource.clip = _shootSounds[Random.Range(0, _shootSounds.Length)];
            _shootSource.volume = Random.Range(0.8f, 1f);
            _shootSource.pitch = Random.Range(0.8f, 1.2f);
            _shootSource.Play();
        }
        void DeathSound()
        {
            enabled = false;
            _attackSource.enabled = false;
            _shootSource.enabled = false;
            _marchingSource.enabled = false;

            _deathGruntSource.pitch = Random.Range(0.8f, 1.2f);
            _deathGruntSource.clip = _deathGruntSounds[Random.Range(0, _deathGruntSounds.Length)];
            _deathGruntSource.Play();

        }
    }
}