using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace StatSystem
{
    [CreateAssetMenu(menuName ="Stats/Level table")]
    public class LevelTable : ScriptableObject
    {
        public List<int> ExperienceToReachLevel;
        public int CurrentLevel(int XP)
        {
            int XpRequired = 0;
            for (int i = 0; i < ExperienceToReachLevel.Count; i++)
            {
                XpRequired += ExperienceToReachLevel[i];
                if (XpRequired > XP)
                    return i + 1;
            }
            return ExperienceToReachLevel.Count;
        }
        public float fractionToNextLevel(int XP)
        {
            int XpRequired = 0;
            for (int i = 0; i < ExperienceToReachLevel.Count; i++)
            {
                float prevXpRequired = XpRequired;
                XpRequired += ExperienceToReachLevel[i];
                if (XpRequired > XP)
                    return (XP - prevXpRequired) / (float)ExperienceToReachLevel[i];
            }
            return 1;
        }

    }
}

