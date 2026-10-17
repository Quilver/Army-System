using UnityEngine;

// The troop's current battle line: a threat-facing line toward nearby enemies, or the line of an ordered
// (manual) formation. Disable to drop the line and stop it updating.
[DisallowMultipleComponent]
public sealed class TroopFormationState : MonoBehaviour
{
    const float HeadingSmoothingTime = 0.8f;
    const float MinimumHeadingSquared = 0.000001f;
    const float MaximumAlignAngle = 100f; // beyond this the lines are not facing each other, so they are left alone

    Troop _troop;
    TroopFormationData _formation;
    bool _hasHeading;
    Vector2 _heading;
    float _distance;

    // Null while this component is disabled.
    public TroopFormationData Formation
    {
        get => enabled ? _formation : null;
        internal set
        {
            if (enabled) _formation = value;
        }
    }

    // The enemy troop this one is currently engaged with (null when not in melee range).
    public Troop EngagedEnemy { get; private set; }
    public bool HasHeading => _hasHeading;
    public Vector2 Heading => _heading;
    public float ThreatDistance => _distance;
    public bool IsThreatFacing => Formation != null && Formation.IsThreatFacing;

    internal void Bind(Troop troop) => _troop = troop;

    void OnDisable()
    {
        _formation = null;
        _hasHeading = false;
    }

    // Called by the combat world each frame with the weighted direction of nearby enemies (null if none).
    // engagedEnemy is the enemy troop this one is in melee range of; while engaged the line may only rotate to line up
    // with that enemy's line (see AlignHeading), never to follow the enemy around.
    internal void UpdateThreat(Vector2? targetHeading, float targetDistance, float deltaTime, Troop engagedEnemy = null,
        Vector2 engagedEnemyFacing = default)
    {
        if (!enabled) return;
        EngagedEnemy = targetHeading.HasValue ? engagedEnemy : null;
        if (!targetHeading.HasValue)
        {
            // Keep a linked line intact while retreating even if the threat has fallen out of range.
            if (_troop.IsFaultSlipping && _hasHeading && (_troop.GetLineLink(0) != null || _troop.GetLineLink(1) != null))
            {
                targetHeading = _heading;
                targetDistance = _distance;
            }
            else
            {
                _hasHeading = false;
                _formation = _troop.HasManualLineOrder ? CreateManualFormation() : null;
                return;
            }
        }

        if (EngagedEnemy != null && _hasHeading)
        {
            AlignHeading(engagedEnemyFacing, deltaTime);
            _distance += (targetDistance - _distance) * (1f - Mathf.Exp(-deltaTime / HeadingSmoothingTime));
        }
        else
        {
            SmoothHeading(targetHeading.Value.normalized, targetDistance, deltaTime);
        }

        // By default the line cuts straight through the troop centre; linking to allied troops moves the endpoints.
        Vector2 center = transform.position;
        Vector2 tangent = new(-_heading.y, _heading.x);
        float halfLength = Mathf.Min(_troop.LineWidth * 0.5f, _troop.Radius * 0.9f);
        _formation = new TroopFormationData
        {
            Start = center - tangent * halfLength,
            End = center + tangent * halfLength,
            Facing = _heading,
            IsThreatFacing = true
        };
    }

    // World position of line end 0 or 1, or the pivot if there is no line.
    public Vector2 SegmentEndpoint(int end)
    {
        if (Formation == null) return transform.position;
        return end == 0 ? _formation.Start : _formation.End;
    }

    // An ordered formation is dropped when the order completes; a threat-facing one stays.
    internal void ClearOrderedFormation()
    {
        if (_formation != null && !_formation.IsThreatFacing) _formation = null;
    }

    // Turns the line toward being parallel to the enemy line (facing opposite its facing). Rotation only ever reduces
    // the angle between the two lines: an aligned pair does not rotate at all, so engaged troops cannot orbit.
    // Both troops apply the same rule, so a skewed pair meets in the middle and their shared orientation is unchanged.
    void AlignHeading(Vector2 enemyFacing, float deltaTime)
    {
        if (enemyFacing.sqrMagnitude < MinimumHeadingSquared) return;
        float current = Mathf.Atan2(_heading.y, _heading.x) * Mathf.Rad2Deg;
        float desired = Mathf.Atan2(-enemyFacing.y, -enemyFacing.x) * Mathf.Rad2Deg;
        float delta = Mathf.DeltaAngle(current, desired);
        if (Mathf.Abs(delta) > MaximumAlignAngle) return;

        float blend = 1f - Mathf.Exp(-deltaTime / HeadingSmoothingTime);
        float angle = (current + delta * blend) * Mathf.Deg2Rad;
        _heading = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
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

    TroopFormationData CreateManualFormation()
    {
        Vector2 center = transform.position;
        Vector2 half = _troop.LineAxis.normalized * (_troop.LineWidth * 0.5f);
        return new TroopFormationData
        {
            Start = center - half,
            End = center + half,
            Facing = transform.right,
            IsThreatFacing = false
        };
    }
}