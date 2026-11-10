namespace BattlePrep.Tent
{
    // Tent Flaps: hover shows whether the army can march out, or why not (spec TentHub.md §2 row 10, §5).
    public class TentFlapsHotspot : TentHotspot
    {
        public override string HoverText
        {
            get
            {
                bool ready = LaunchRules.CanLaunch(out string reason);
                string colour = ready ? "#9BE79B" : "#F2A27A";
                return $"<b>{DisplayName}</b>\n<color={colour}>{reason}</color>";
            }
        }
    }
}
