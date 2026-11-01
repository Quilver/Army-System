using UnityEngine;

namespace Campaign
{
    // A character in the player's roster, built on the new character system (CampaignCharacter, spec §1.1). Holds the
    // campaign progress the asset does not: level, XP and the gold it costs to field. Pre-battle tent: TentHub.md SU-52.
    [System.Serializable]
    public class RosterCharacter
    {
        public const int XpPerLevel = 100;

        public CampaignCharacter character;
        [Min(1)] public int level = 1;
        [Range(0, XpPerLevel - 1)] public int xp;
        [Range(5, 50)] public int costToField = 20;

        public string Name => character != null ? character.CharacterName : "Unknown";
        public string ClassName => character != null ? character.ClassName : "";
        public Sprite Portrait => character != null ? character.Portrait : null;
        public float XpFraction => Mathf.Clamp01(xp / (float)XpPerLevel);
    }
}
