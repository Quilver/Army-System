using ModelComponents;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AudioSystem
{
    public class ModelSoundEffects : MonoBehaviour
    {
        [SerializeField]
        AudioClip[] _attackSounds;
        [SerializeField]
        AudioSource _marchingSource, _attackSource, _shootSource;

        Rigidbody2D _body;
        private void Start()
        {
            _body = GetComponentInParent<Rigidbody2D>();
            transform.parent.GetComponentInChildren<IMeleeAttack>().Strike += StrikeSound;
            transform.parent.GetComponentInChildren<Shooter>().Shot += ShootSound;
        }

        void Update()
        {
            MarchSound();
        }
        void MarchSound()
        {
            if (_body.velocity.magnitude > 0.1f)
            {
                _marchingSource.volume = Mathf.Clamp(_body.velocity.magnitude, 0.1f, 0.3f);
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
            _shootSource.volume = Random.Range(0.8f, 1f);
            _shootSource.pitch = Random.Range(0.8f, 1.2f);
            _shootSource.Play();
        }
    }
}