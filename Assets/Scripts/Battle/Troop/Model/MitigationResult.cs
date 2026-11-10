// Result of rolling one hit against a defender's armour (spec §4.6). Pure data, no Unity types.
//   Deflected: points stopped by the armour.
//   Bruised:   points that become bruise (heals over time).
//   Bled:      rolled points that become bleed.
//   Overflow:  points above TotalDefence. These always become bleed, and count as shock like any other bleed (SU-128).
// ShockPoints is every point that becomes bruise or bleed (Bruised + Bled + Overflow); only Deflected adds no shock.
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

    public int ShockPoints => Bruised + Bled + Overflow;
}

// Points actually applied to a soldier after its HP clamp. Shock is every applied point of bruise and bleed (overflow
// included, SU-128); damage past zero HP is not applied and adds no shock.
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
