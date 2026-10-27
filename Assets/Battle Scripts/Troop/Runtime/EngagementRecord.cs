// One opposing troop near this troop, measured this frame (spec §4.1, §4.8).
//   Gap      footprint gap between the two troops (the single engagement metric, Q-16).
//   Face     which of this troop's faces the opponent is attacking (Q-31).
//   Engaged  Gap <= A_Distance.
public readonly struct EngagementRecord
{
    public readonly Troop Opponent;
    public readonly float Gap;
    public readonly TroopFace Face;
    public readonly bool Engaged;

    public EngagementRecord(Troop opponent, float gap, TroopFace face, bool engaged)
    {
        Opponent = opponent;
        Gap = gap;
        Face = face;
        Engaged = engaged;
    }
}
