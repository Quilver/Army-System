namespace BattlePrep.Tent
{
    // Debts Stack: hover shows the number of open repayment orders (spec TentHub.md §2 row 4).
    public class DebtsHotspot : TentHotspot
    {
        public override string HoverText
        {
            get
            {
                int open = Campaign.CampaignDataManager.Data.BuyOrders?.Count ?? 0;
                string count = open == 0 ? "No open repayments" : open == 1 ? "1 open repayment" : $"{open} open repayments";
                return $"<b>{DisplayName}</b>\n{count}";
            }
        }
    }
}
