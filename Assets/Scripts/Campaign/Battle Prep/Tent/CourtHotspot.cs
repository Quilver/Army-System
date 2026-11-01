namespace BattlePrep.Tent
{
    // A court member: hover shows their name; the dispatch-bearer also mentions the sealed dispatch when a quest is
    // available (spec TentHub.md §2 rows 5-7).
    public class CourtHotspot : TentHotspot
    {
        public override string HoverText
        {
            get
            {
                var hub = TentHubController.Instance;
                string name = hub != null ? hub.CourtName(CourtIndex) : DisplayName;
                bool quest = hub != null && hub.HasQuestCue(CourtIndex);
                return quest ? $"<b>{name}</b>\n<i>Holds a sealed dispatch</i>" : $"<b>{name}</b>";
            }
        }
    }
}
