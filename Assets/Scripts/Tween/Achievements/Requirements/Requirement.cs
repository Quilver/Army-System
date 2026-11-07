using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace AchievementSystem
{
    // What must happen in a battle for an achievement to be earned (SU-114). Each battle works on its own copy (the
    // achievement asset is instantiated), so state lives in [NonSerialized] fields and starts fresh every battle.
    // Lifecycle, driven by the AchievementController:
    //   Begin   once, when the battle's achievements are set up: subscribe to events (OnBegin).
    //   Tick    every frame of battle time (OnTick), for things measured by polling (melee time, who we are fighting).
    //   Finish  once, when the battle ends (OnBattleEnded), then unsubscribe (OnEnd). The level is frozen after this.
    //   Abort   unsubscribe without finishing (the battle scene closed early).
    // Level: 0 = not earned; 1 or more = earned, and the rewards are multiplied by it.
    [Serializable]
    public abstract class Requirement
    {
        [NonSerialized] AchievementContext _context;
        [NonSerialized] bool _tracking;
        [NonSerialized] bool _finished;

        protected AchievementContext Context => _context;
        public bool IsTracking => _tracking;
        public bool IsFinished => _finished;

        public bool Begin(AchievementContext context)
        {
            if (_tracking || _finished) return _tracking;
            _context = context;
            if (!CanTrack(context)) return false;
            _tracking = true;
            OnBegin();
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (_tracking && deltaTime > 0f) OnTick(deltaTime);
        }

        public void Finish(bool victory)
        {
            if (!_tracking) return;
            OnBattleEnded(victory);
            Stop();
            _finished = true;
        }

        public void Abort()
        {
            if (_tracking) Stop();
        }

        void Stop()
        {
            _tracking = false;
            OnEnd();
        }

        protected virtual bool CanTrack(AchievementContext context) => context != null;
        protected virtual void OnBegin() { }
        protected virtual void OnTick(float deltaTime) { }
        protected virtual void OnBattleEnded(bool victory) { }
        protected virtual void OnEnd() { }

        public abstract int Level { get; }

        // Optional progress text for the UI ("3/5"), or null.
        public virtual string Progress => null;
    }

    // Measured on the troop a deployed character leads. Not tracked when the character has no troop in the battle.
    [Serializable]
    public abstract class CharacterRequirement : Requirement
    {
        protected Troop Troop => Context?.Troop;
        protected Campaign.RosterCharacter Character => Context?.Character;

        protected override bool CanTrack(AchievementContext context) => context != null && context.Troop != null;

        protected bool IsEnemy(Troop other)
        {
            Troop troop = Troop;
            return other != null && troop != null && other.TeamId != troop.TeamId;
        }

        // Our troop is in melee with the other troop this frame (either side's engagement records).
        protected bool InContactWith(Troop other)
        {
            Troop troop = Troop;
            if (troop == null || other == null) return false;
            return troop.WasEngagedWith(other) || other.WasEngagedWith(troop);
        }

        // Credit for something that happened to an enemy troop (a break, a defeat): our troop dealt the last damaging hit
        // (cause), or is in melee with it. A troop that has been destroyed gets no credit.
        protected bool IsCredited(Troop enemy, Troop cause)
        {
            Troop troop = Troop;
            if (troop == null || !IsEnemy(enemy)) return false;
            return ReferenceEquals(cause, troop) || InContactWith(enemy);
        }
    }

    // Measured for the whole battle.
    [Serializable]
    public abstract class GlobalRequirement : Requirement { }

    // A character requirement that counts events ("make 5 kills", "rout an enemy troop"). Earned at Required; when
    // Repeatable, earned again for every further Required (up to Max Level), and the rewards are multiplied.
    [Serializable]
    public abstract class CountedRequirement : CharacterRequirement
    {
        [SerializeField, Min(1), FormerlySerializedAs("killsRequired"), Tooltip("How many times it must happen.")]
        int _required = 1;
        [SerializeField, Tooltip("Earned again for every further Required times; the rewards are multiplied.")]
        bool _repeatable;
        [SerializeField, Min(1), Tooltip("Highest level when repeatable.")]
        int _maxLevel = 3;

        [NonSerialized] int _count;

        public int Required => Mathf.Max(1, _required);
        public int Count => _count;

        protected void Add(int amount = 1) => _count += Mathf.Max(0, amount);

        public override int Level => AchievementCount.Level(_count, _required, _repeatable, _maxLevel);
        public override string Progress => $"{Mathf.Min(_count, Required)}/{Required}";
    }

    // Pure level rules, shared and tested (AchievementTests).
    public static class AchievementCount
    {
        public static int Level(int count, int required, bool repeatable, int maxLevel)
        {
            required = Mathf.Max(1, required);
            if (count < required) return 0;
            if (!repeatable) return 1;
            return Mathf.Clamp(count / required, 1, Mathf.Max(1, maxLevel));
        }
    }
}
