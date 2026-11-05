using UnityEngine;

namespace Campaign
{
    // A character in the player's roster, built on the new character system (CampaignCharacter, spec §1.1). Holds the
    // campaign progress the asset does not: level, XP and the gold it costs to field. Pre-battle tent: TentHub.md SU-52.
    [System.Serializable]
    public class RosterCharacter : ISerializationCallbackReceiver
    {
        public const int XpPerLevel = 100;
        // Resources folder of the campaign characters; saves refer to a character by its asset name in this folder.
        public const string CharacterFolder = "Troop and Character Profiles/Characters/Campaign";

        public CampaignCharacter character;
        [SerializeField, HideInInspector] string characterAsset;
        [Min(1)] public int level = 1;
        [Range(0, XpPerLevel - 1)] public int xp;
        [Range(5, 50)] public int costToField = 20;

        public event System.Action<int> GainedXp;
        public event System.Action LevelledUp;

        public string Name => character != null ? character.CharacterName : "Unknown";
        public string ClassName => character != null ? character.ClassName : "";
        public Sprite ClassIcon => character != null ? character.ClassIcon : null;
        public Sprite Portrait => character != null ? character.Portrait : null;
        public float XpFraction => Mathf.Clamp01(xp / (float)XpPerLevel);
        // XP earned since level 1.
        public int TotalXp => (level - 1) * XpPerLevel + xp;

        public static float FractionToNextLevel(int totalXp) => (totalXp % XpPerLevel) / (float)XpPerLevel;

        // Adds XP and levels up every XpPerLevel. Stat growth on level-up is not applied yet (SU entry, open item).
        public void AddXp(int amount)
        {
            if (amount <= 0) return;
            GainedXp?.Invoke(amount);
            xp += amount;
            while (xp >= XpPerLevel)
            {
                xp -= XpPerLevel;
                level++;
                LevelledUp?.Invoke();
            }
        }

        // A saved game stores the asset name only (object references do not survive a JSON save). Called after loading.
        public void ResolveCharacter()
        {
            if (character != null || string.IsNullOrEmpty(characterAsset)) return;
            character = Resources.Load<CampaignCharacter>(CharacterFolder + "/" + characterAsset);
            if (character == null) Debug.LogWarning($"Saved roster character '{characterAsset}' was not found in Resources/{CharacterFolder}.");
        }

        public void OnBeforeSerialize()
        {
            if (character != null) characterAsset = character.name;
        }

        public void OnAfterDeserialize() { }
    }
}
