using UnityEngine;

namespace BattlePrep.Tent
{
    // Mock campaign + mission used when the Battle Preparation scene is played directly, without coming from the main
    // menu (no CampaignDataManager). Never saved to disk. Generate one with
    // Leveraged Wars > Pre-Battle > Create Tent Sample Data.
    [CreateAssetMenu(menuName = "Preparations/Tent sample data")]
    public class TentSampleData : ScriptableObject
    {
        [Tooltip("Gold, prestige, level, open repayments and roster for the sample.")]
        public Campaign.CampaignData campaign = new();
        [Tooltip("Mission used by the sample: briefing, loan offers, court.")]
        public PreparationData mission;

        // A copy, so playing the sample never changes this asset.
        public Campaign.CampaignData CloneCampaign() =>
            JsonUtility.FromJson<Campaign.CampaignData>(JsonUtility.ToJson(campaign));
    }
}
