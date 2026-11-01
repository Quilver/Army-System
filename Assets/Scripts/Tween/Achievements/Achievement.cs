using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AchievementSystem
{
    public abstract class Achievement : ScriptableObject
    {
        //Setup general
        public Sprite icon;
        
        public abstract string Description { get; }

        public abstract bool Achieved();
        public abstract void Reward();
    }
}
