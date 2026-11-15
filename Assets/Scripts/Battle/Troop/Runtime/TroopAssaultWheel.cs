using UnityEngine;

// The assault wheel's state for one troop (SU-163): which face of its pursuit target it squares to, or, when the two
// troops pursue each other, the axis they both square to. Kept from the frame the troop engages its target until it is
// no longer engaged with it (Reset). The wheel itself is TroopFormationState.AssaultWheel.
//   Line target: the face is chosen when they engage (FaceClassifier's boundaries) and, with Assault Face Hold on, kept
//   until the attacker's bearing is AssaultFaceHysteresisDeg past a boundary (AssaultWheelRules.KeepFace).
//   Blob or column target: square to the direction of the target's pivot.
//   Mutual pursuit (Mutual Pursuit Front To Front): the line between their pivots when they engaged; the second troop
//   takes the first one's axis reversed, so both square to the same line and meet front to front.
public sealed class TroopAssaultWheel
{
    Troop _target;
    bool _hasFace;
    TroopFace _face;
    bool _hasAxis;
    Vector2 _axis;

    public Troop Target => _target;
    public bool HasFace => _hasFace;
    public TroopFace Face => _face;

    public void Reset()
    {
        _target = null;
        _hasFace = false;
        _hasAxis = false;
    }

    // The pair's axis this troop squares to while it and target pursue each other, if it has taken one.
    public bool TryGetAxisAgainst(Troop target, out Vector2 axis)
    {
        axis = _axis;
        return _hasAxis && _target == target;
    }

    // The heading self (a pursuer engaged with target) wheels to this frame.
    public Vector2 TargetHeading(Troop self, Troop target, TroopTuning tuning)
    {
        if (target != _target)
        {
            Reset();
            _target = target;
        }
        Vector2 ownPivot = self.transform.position;
        Vector2 targetPivot = target.transform.position;
        Vector2 toTarget = targetPivot - ownPivot;
        toTarget = toTarget.sqrMagnitude > 1e-8f ? toTarget.normalized : self.Facing;

        if (IsMutual(self, target, tuning))
        {
            if (!_hasAxis)
            {
                _axis = target.AssaultWheel.TryGetAxisAgainst(self, out Vector2 theirs) ? -theirs : toTarget;
                _hasAxis = true;
            }
            return _axis;
        }
        _hasAxis = false;

        TroopFootprint theirsFootprint = target.Footprint;
        if (theirsFootprint.Kind != FootprintKind.Rect)
        {
            _hasFace = false;
            return toTarget;
        }

        float bearing = AssaultWheelRules.Bearing(theirsFootprint, targetPivot, self.Footprint);
        _face = _hasFace && tuning.AssaultFaceHold
            ? AssaultWheelRules.KeepFace(_face, bearing, tuning.FrontFaceHalfAngleDeg, tuning.RearFaceAngleDeg,
                tuning.AssaultFaceHysteresisDeg)
            : AssaultWheelRules.FaceAt(bearing, tuning.FrontFaceHalfAngleDeg, tuning.RearFaceAngleDeg);
        _hasFace = true;
        return AssaultWheelRules.SquareTo(theirsFootprint, _face);
    }

    // Two troops pursuing each other (SU-163), neither routed.
    public static bool IsMutual(Troop self, Troop target, TroopTuning tuning) =>
        tuning.MutualPursuitFrontToFront && target != null && target.PursuitTarget == self && !target.IsRouted
        && !self.IsRouted;
}
