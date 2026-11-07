using System;
using System.Collections.Generic;
using UnityEngine;

// Character requirements built on the troop combat events (BattleEvents.TroopBroke, TroopDefeated) and on polling the
// troop's melee state (SU-114). "Credited" means the character's troop dealt the enemy's last damaging hit or is in
// melee with it at that moment (CharacterRequirement.IsCredited).
namespace AchievementSystem
{
    // "Rout an enemy troop": an enemy troop routs (spec §5.4) and the character's troop is credited with it.
    [Serializable]
    public class RoutEnemy : CountedRequirement
    {
        [SerializeField, Tooltip("Also count enemy troops wiped out to the last soldier before they could rout.")]
        bool _countWipedOut;

        protected override void OnBegin()
        {
            BattleEvents.TroopBroke += OnTroopBroke;
            BattleEvents.TroopDefeated += OnTroopDefeated;
        }

        protected override void OnEnd()
        {
            BattleEvents.TroopBroke -= OnTroopBroke;
            BattleEvents.TroopDefeated -= OnTroopDefeated;
        }

        void OnTroopBroke(TroopBreakInfo info)
        {
            if (info.Kind == TroopBreakKind.Rout && IsCredited(info.Troop, info.Cause)) Add();
        }

        // A rout raises TroopDefeated too; only a troop that died without routing is counted here.
        void OnTroopDefeated(Troop enemy)
        {
            if (!_countWipedOut || enemy == null || enemy.IsRouted) return;
            if (IsCredited(enemy, enemy.LastAttacker)) Add();
        }
    }

    // "Defeat a troop despite being outnumbered": an enemy troop that outnumbered the character's troop at any moment
    // while the two were in melee is later defeated (routed or wiped out), credited to the character's troop.
    // Outnumbered: the enemy's living soldiers > Ratio x ours. Only the two troops count, not allies helping either side.
    [Serializable]
    public class DefeatWhileOutnumbered : CountedRequirement
    {
        [SerializeField, Min(1f), Tooltip("The enemy must have more than this many times our living soldiers.")]
        float _ratio = 1f;

        [NonSerialized] HashSet<Troop> _outnumberedBy;

        protected override void OnBegin()
        {
            _outnumberedBy = new HashSet<Troop>();
            BattleEvents.TroopDefeated += OnTroopDefeated;
        }

        protected override void OnEnd() => BattleEvents.TroopDefeated -= OnTroopDefeated;

        protected override void OnTick(float deltaTime)
        {
            Troop troop = Troop;
            if (troop == null || troop.IsRouted) return;
            foreach (var record in troop.Engagements)
            {
                if (!record.Engaged || record.Opponent == null) continue;
                if (Outnumbers(record.Opponent.LivingSoldierCount, troop.LivingSoldierCount, _ratio))
                    _outnumberedBy.Add(record.Opponent);
            }
        }

        void OnTroopDefeated(Troop enemy)
        {
            if (enemy == null || !_outnumberedBy.Contains(enemy)) return;
            // Also checked here: a defeat in the same frame the melee began is not seen by OnTick.
            if (IsCredited(enemy, enemy.LastAttacker)) Add();
            _outnumberedBy.Remove(enemy);
        }

        public static bool Outnumbers(int enemySoldiers, int ourSoldiers, float ratio) =>
            ourSoldiers > 0 && enemySoldiers > ourSoldiers * Mathf.Max(1f, ratio);
    }

    // "Cause an enemy troop to retreat on the initial assault": the character's troop attacks with an order (Assault,
    // spec §4.2) and an enemy troop it meets in melee for the first time retreats (or routs) within Window seconds of that
    // first contact, before our troop leaves melee. Only the first melee with each enemy troop counts.
    [Serializable]
    public class BreakOnInitialAssault : CountedRequirement
    {
        [SerializeField, Min(1f), Tooltip("Seconds after first contact during which a break still counts.")]
        float _windowSeconds = 15f;
        [SerializeField, Tooltip("Only when our troop attacked with an order (Assault). Off: any first contact counts, " +
            "including being charged.")]
        bool _requireAssault = true;
        [SerializeField, Tooltip("A rout counts as well as a retreat.")]
        bool _countRouts = true;

        [NonSerialized] HashSet<Troop> _met;
        [NonSerialized] Dictionary<Troop, float> _open;
        [NonSerialized] List<Troop> _keys;

        protected override void OnBegin()
        {
            _met = new HashSet<Troop>();
            _open = new Dictionary<Troop, float>();
            _keys = new List<Troop>();
            BattleEvents.TroopBroke += OnTroopBroke;
        }

        protected override void OnEnd() => BattleEvents.TroopBroke -= OnTroopBroke;

        bool Attacking => !_requireAssault || (Troop != null && Troop.IsAssaulting);

        protected override void OnTick(float deltaTime)
        {
            Troop troop = Troop;
            if (troop == null || troop.IsRouted || !troop.IsEngaged)
            {
                _open.Clear();  // contact lost: the initial assault is over
                return;
            }

            _keys.Clear();
            _keys.AddRange(_open.Keys);
            foreach (var enemy in _keys)
            {
                float left = _open[enemy] - deltaTime;
                if (left <= 0f) _open.Remove(enemy);
                else _open[enemy] = left;
            }

            foreach (var record in troop.Engagements)
            {
                if (!record.Engaged || record.Opponent == null || !_met.Add(record.Opponent)) continue;
                if (Attacking) _open[record.Opponent] = _windowSeconds;
            }
        }

        void OnTroopBroke(TroopBreakInfo info)
        {
            bool counts = info.Kind == TroopBreakKind.Retreat || (_countRouts && info.Kind == TroopBreakKind.Rout);
            if (!counts || info.Troop == null) return;
            // A break in the very frame of first contact comes before OnTick has seen the contact.
            bool initial = _open.ContainsKey(info.Troop) ||
                           (!_met.Contains(info.Troop) && InContactWith(info.Troop) && Attacking);
            _met.Add(info.Troop);
            _open.Remove(info.Troop);
            if (initial && IsCredited(info.Troop, info.Cause)) Add();
        }
    }

    // "Last in a melee engagement for at least 1 minute": the character's troop stays in melee for Seconds of battle time
    // without breaking. Gaps shorter than Grace seconds (a push-back, a re-charge) do not end the engagement; a retreat
    // or rout of our troop does.
    [Serializable]
    public class HoldMelee : CharacterRequirement
    {
        [SerializeField, Min(1f), Tooltip("Seconds of battle time in one melee.")]
        float _seconds = 60f;
        [SerializeField, Min(0f), Tooltip("Out-of-melee gaps shorter than this keep the engagement going.")]
        float _graceSeconds = 2f;

        [NonSerialized] MeleeHoldTimer _timer;

        protected override void OnBegin()
        {
            _timer = new MeleeHoldTimer(_graceSeconds);
            BattleEvents.TroopBroke += OnTroopBroke;
        }

        protected override void OnEnd() => BattleEvents.TroopBroke -= OnTroopBroke;

        protected override void OnTick(float deltaTime)
        {
            Troop troop = Troop;
            _timer.Tick(troop != null && troop.InMelee && !troop.IsRouted, deltaTime);
        }

        void OnTroopBroke(TroopBreakInfo info)
        {
            if (!ReferenceEquals(info.Troop, Troop)) return;
            if (info.Kind == TroopBreakKind.Retreat || info.Kind == TroopBreakKind.Rout) _timer.Break();
        }

        public override int Level => _timer != null && _timer.Longest >= _seconds ? 1 : 0;
        public override string Progress =>
            $"{Mathf.FloorToInt(Mathf.Min(_timer != null ? _timer.Longest : 0f, _seconds))}/{Mathf.RoundToInt(_seconds)}s";
    }

    // Continuous melee time with a grace period for short gaps (pure, tested in AchievementTests).
    public sealed class MeleeHoldTimer
    {
        readonly float _grace;
        float _gap;

        public MeleeHoldTimer(float graceSeconds) => _grace = Mathf.Max(0f, graceSeconds);

        public float Current { get; private set; }
        public float Longest { get; private set; }

        public void Tick(bool inMelee, float deltaTime)
        {
            if (inMelee)
            {
                Current += deltaTime;
                _gap = 0f;
                if (Current > Longest) Longest = Current;
                return;
            }
            if (Current <= 0f) return;
            _gap += deltaTime;
            if (_gap > _grace) Break();
        }

        public void Break()
        {
            Current = 0f;
            _gap = 0f;
        }
    }

    // "Disorder an enemy with ranged attacks": an enemy troop is disordered (SU-104) and the hit that broke it was a
    // projectile from the character's troop.
    [Serializable]
    public class RangedDisorder : CountedRequirement
    {
        [SerializeField, Tooltip("A rout or retreat caused by our projectiles counts as well.")]
        bool _countOtherBreaks;

        protected override void OnBegin() => BattleEvents.TroopBroke += OnTroopBroke;
        protected override void OnEnd() => BattleEvents.TroopBroke -= OnTroopBroke;

        void OnTroopBroke(TroopBreakInfo info)
        {
            if (!info.CauseWasRanged || !ReferenceEquals(info.Cause, Troop) || Troop == null || !IsEnemy(info.Troop))
                return;
            if (info.Kind == TroopBreakKind.Disorder || (_countOtherBreaks && info.Kind != TroopBreakKind.SurroundedBreak))
                Add();
        }
    }
}
