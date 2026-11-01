using UnityEngine;

namespace AudioSystem
{
    // Battle sound effects for the troop system. One per battle scene: plays a strike sound for melee hits that deal
    // damage, a shot sound for each volley, a death grunt for each soldier killed and a hit sound for each projectile
    // that hits. Each sound type is rate limited so a large melee does not restart the same source every frame.
    public class ModelSoundEffects : MonoBehaviour
    {
        [SerializeField]
        AudioClip[] _attackSounds, _shootSounds, _deathGruntSounds, _projectileHitSounds;
        [SerializeField]
        AudioSource _attackSource, _shootSource, _deathGruntSource, _projectileHitSource;
        [SerializeField, Min(0f), Tooltip("Shortest time between two sounds of the same type.")]
        float _minimumInterval = 0.08f;

        float _nextAttack, _nextShot, _nextDeath, _nextHit;

        void OnEnable()
        {
            BattleEvents.SoldierStruck += StrikeSound;
            BattleEvents.VolleyFired += ShootSound;
            BattleEvents.SoldierKilled += DeathSound;
            BattleEvents.ProjectileLanded += ProjectileSound;
        }

        void OnDisable()
        {
            BattleEvents.SoldierStruck -= StrikeSound;
            BattleEvents.VolleyFired -= ShootSound;
            BattleEvents.SoldierKilled -= DeathSound;
            BattleEvents.ProjectileLanded -= ProjectileSound;
        }

        void StrikeSound(Troop attacker, Troop victim, Vector2 position) =>
            Play(_attackSource, _attackSounds, ref _nextAttack, 1f);

        void ShootSound(Troop shooter) =>
            Play(_shootSource, _shootSounds, ref _nextShot, Random.Range(0.8f, 1f));

        void DeathSound(Troop victim, SoldierAgentState soldier, Troop attacker) =>
            Play(_deathGruntSource, _deathGruntSounds, ref _nextDeath, 1f);

        void ProjectileSound(Troop shooter, Troop target, bool hit, Vector2 position)
        {
            if (hit) Play(_projectileHitSource, _projectileHitSounds, ref _nextHit, 1f);
        }

        void Play(AudioSource source, AudioClip[] clips, ref float next, float volume)
        {
            if (source == null || clips == null || clips.Length == 0 || Time.time < next) return;
            next = Time.time + _minimumInterval;
            source.clip = clips[Random.Range(0, clips.Length)];
            source.volume = volume;
            source.pitch = Random.Range(0.8f, 1.2f);
            source.Play();
        }
    }
}
