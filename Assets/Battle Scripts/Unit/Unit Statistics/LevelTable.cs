using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace StatSystem
{
    [CreateAssetMenu(menuName ="Stats/Level table")]
    public class LevelTable : ScriptableObject
    {
        public List<int> ExperienceToReachLevel;
    }
}

