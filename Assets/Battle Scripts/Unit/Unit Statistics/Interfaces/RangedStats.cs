using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace StatSystem
{
    public interface IRangedStats
    {
        public int ReloadRate { get; }
        public int Accuracy { get; }
    }
}

