namespace BattlePrep.Tent
{
    // Gold Chest: hover shows the exact gold balance and prestige (spec TentHub.md §2 row 1, PB-Q1, PB-Q6).
    public class GoldChestHotspot : TentHotspot
    {
        public override string HoverText
        {
            get
            {
                var data = Campaign.CampaignDataManager.Data;
                return $"<b>Treasury</b>\nGold: {data.Money}\nPrestige: {data.CurrentPrestige}/{data.MaxPrestige}";
            }
        }
    }
}
