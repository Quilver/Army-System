// Result of rolling one hit against a defender's armour (spec §4.6). Pure data, no Unity types.
//   Deflected: points stopped by the armour.
//   Bruised:   points that become bruise (heals over time).
//   Bled:      rolled points that become bleed.
//   Overflow:  points above TotalDefence. These always become bleed, but do not count as shock.
// ShockPoints is the rolled part only (Bruised + Bled), so overflow never adds shock.
public readonly struct MitigationResult
{
    public readonly int Deflected;
    public readonly int Bruised;
    public readonly int Bled;
    public readonly int Overflow;

    public MitigationResult(int deflected, int bruised, int bled, int overflow)
    {
        Deflected = deflected;
        Bruised = bruised;
        Bled = bled;
        Overflow = overflow;
    }

    public int ShockPoints => Bruised + Bled;
}

// Points actually applied to a soldier after its HP clamp. Shock is the applied rolled bleed plus applied bruise. Overflow
// is excluded, and so is any damage past zero HP.
public readonly struct AppliedDamage
{
    public readonly int Bruised;
    public readonly int Bled;
    public readonly int Shock;

    public AppliedDamage(int bruised, int bled, int shock)
    {
        Bruised = bruised;
        Bled = bled;
        Shock = shock;
    }

    public int Total => Bruised + Bled;
}
