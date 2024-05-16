using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AchievementSystem
{
    public abstract class Achievments : ScriptableObject
    {
        public Sprite icon;
        public abstract void Initialise();
        public abstract string Description { get; }
        public abstract bool Achieved();
        public abstract void Reward();

    }
}