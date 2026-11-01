// When two opposing soldiers in contact clash (spec §4.5). Pure C#.
//   (a) a charger in contact with an enemy soldier clashes at once; the charge ends.
//   (b) two soldiers in contact, neither charging, clash when both are off their per-soldier clash cooldown (Q-26).
//   Charge followers never start a clash of their own (spec §4.4). They only clash when a charger reaches them.
public enum ClashKind
{
    None,
    Charge,  // case (a)
    Contact  // case (b)
}

public static class ClashRules
{
    public static ClashKind Classify(bool aCharging, bool bCharging, bool aFollowing, bool bFollowing,
        float aCooldownRemaining, float bCooldownRemaining)
    {
        if (aCharging || bCharging) return ClashKind.Charge;
        if (aFollowing || bFollowing) return ClashKind.None;
        if (aCooldownRemaining > 0f || bCooldownRemaining > 0f) return ClashKind.None;
        return ClashKind.Contact;
    }
}
