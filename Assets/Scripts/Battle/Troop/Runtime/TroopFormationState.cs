using UnityEngine;

// The troop's battle line (spec §3.3). Each frame it builds the troop's natural line through its pivot:
//   - holding or marching under a player line order: the ordered facing and length;
//   - otherwise, with an enemy within S_Distance: facing the threat at the natural length L_nat.
// While the troop is engaged its facing is locked (spec §3.3, §4.1): the line does not rotate toward a moving enemy.
// The chain step (ChainLinker) then joins linked line points and publishes the final Formation.
// Disable to drop the line and stop it updating.
[DisallowMultipleComponent]
public sealed class TroopFormationState : MonoBehaviour
{
    const float HeadingSmoothingTime = 0.8f;
    const float MinimumHeadingSquared = 0.000001f;

    Troop _troop;
    TroopFormationData _natural;
    TroopFormationData _formation;
    bool _hasHeading;
    bool _hasThreat;
    Vector2 _heading;
    float _distance;

    // The line the troop is using this frame, after chain joints (null without a line, or while disabled).
    public TroopFormationData Formation
    {
        get => enabled ? _formation : null;
        internal set
        {
            if (enabled) _formation = value;
        }
    }

    // The troop's own line before chain joints are applied.
    public TroopFormationData NaturalFormation => enabled ? _natural : null;

    public bool HasHeading => _hasHeading;
    // An enemy is within S_Distance (footprint gap), so the troop forms a line facing it.
    public bool HasThreat => _hasThreat;
    public Vector2 Heading => _heading;
    public float ThreatDistance => _distance;
    public bool IsThreatFacing => Formation != null && Formation.IsThreatFacing;

    internal void Bind(Troop troop) => _troop = troop;

    void OnDisable()
    {
        _formation = null;
        _natural = null;
        _hasHeading = false;
        _hasThreat = false;
    }

    // Called by the combat world each frame. threatHeading is the weighted direction of enemies within S_Distance (null
    // if none). While engaged the heading is locked.
    internal void UpdateNatural(Vector2? threatHeading, float threatDistance, bool engaged, float deltaTime)
    {
        if (!enabled) return;
        _hasThreat = threatHeading.HasValue;
        if (_troop.IsRouted)
        {
            _natural = null;
            _formation = null;
            _hasHeading = false;
            return;
        }

        if (threatHeading.HasValue && !engaged)
            SmoothHeading(threatHeading.Value.normalized, threatDistance, deltaTime);
        else if (threatHeading.HasValue && !_hasHeading)
            SmoothHeading(threatHeading.Value.normalized, threatDistance, deltaTime);

        TroopLineOrder order = _troop.LineOrder;
        bool ordered = order != null && order.IsActive;
        bool holdingOrder = ordered && order.HasIdleLine && !_troop.IsMoving;

        Vector2 facing;
        bool threatFacing;
        if (ordered && (holdingOrder || _hasThreat))
        {
            facing = order.Facing;
            threatFacing = _hasThreat;
        }
        else if (_hasThreat && _hasHeading)
        {
            facing = _heading;
            threatFacing = true;
        }
        else
        {
            _natural = null;
            _formation = null;
            if (!_hasThreat) _hasHeading = false;
            return;
        }

        float length = ordered ? order.Length : _troop.NaturalLineLength;
        _natural = Build(transform.position, facing, length, threatFacing);
    }

    public static TroopFormationData Build(Vector2 center, Vector2 facing, float length, bool threatFacing)
    {
        if (facing.sqrMagnitude < MinimumHeadingSquared) facing = Vector2.right;
        facing.Normalize();
        Vector2 tangent = new(-facing.y, facing.x);
        Vector2 half = tangent * (length * 0.5f);
        return new TroopFormationData
        {
            Start = center - half,
            End = center + half,
            Facing = facing,
            IsThreatFacing = threatFacing
        };
    }

    // World position of line point 0 or 1, or the pivot if there is no line.
    public Vector2 SegmentEndpoint(int end)
    {
        if (Formation == null) return transform.position;
        return end == 0 ? _formation.Start : _formation.End;
    }

    void SmoothHeading(Vector2 target, float targetDistance, float deltaTime)
    {
        if (!_hasHeading)
        {
            _heading = target;
            _distance = targetDistance;
            _hasHeading = true;
            return;
        }

        float blend = 1f - Mathf.Exp(-deltaTime / HeadingSmoothingTime);
        Vector2 smoothed = Vector2.Lerp(_heading, target, blend);
        if (smoothed.sqrMagnitude > MinimumHeadingSquared) _heading = smoothed.normalized;
        _distance += (targetDistance - _distance) * blend;
    }
}
