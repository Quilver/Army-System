using UnityEngine;

// The outline a troop shows and is picked by (spec §6.1). It is drawn from the troop's formation, the shape its soldiers
// are steered into (SoldierFieldPlanner), not from where each soldier happens to stand, so it does not pulse as soldiers
// sway, jostle and charge:
//   Line / Chain  the battle line from line point to line point, as deep as its ranks (LineGeometry.RankHalfWidth).
//   Column        the marching column trailing the pivot, along a smoothed march heading. Its length and width are the
//                 soldier field's (LineGeometry column helpers, SU-120), so it hugs the column and is longer than wide.
//   Idle          the blob disc (BlobRadiusScale x TroopRadius).
//   Rout          the scatter disc.
// Each is padded by one soldier radius. The outline is kept relative to the pivot, so a marching troop's outline moves
// exactly with it; changes of shape, size and facing ease in over ShapeSmoothTime. The rules keep using the footprint
// (TroopFootprintBuilder); this is for display and picking only.
public sealed class TroopOutline
{
    // Seconds for the outline's size, facing and place around the pivot to close most of the gap to the formation.
    public const float ShapeSmoothTime = 0.2f;
    // Seconds for the column outline to turn toward the march heading.
    public const float HeadingSmoothTime = 0.35f;
    // A column must be still this long before its outline gathers into the blob (rides out stop-start pathing).
    public const float StopHoldSeconds = 0.3f;
    const float Padding = TroopConstants.SoldierRadius;

    bool _seeded;
    FootprintKind _kind;
    Vector2 _offset;
    float _facingAngle;
    float _halfLength;
    float _halfWidth;
    float _rounding;

    bool _marching;
    float _stillTime;
    Vector2 _heading = Vector2.right;

    public bool IsSeeded => _seeded;
    public bool IsMarching => _marching;
    public Vector2 Heading => _heading;

    public void Reset()
    {
        _seeded = false;
        _marching = false;
        _stillTime = 0f;
    }

    // The outline around a pivot position.
    public TroopFootprint At(Vector2 pivot)
    {
        float radians = _facingAngle * Mathf.Deg2Rad;
        Vector2 facing = new(Mathf.Cos(radians), Mathf.Sin(radians));
        return new TroopFootprint(_kind, pivot + _offset, facing, _halfLength, _halfWidth, _rounding);
    }

    public void Update(Troop troop, float deltaTime)
    {
        Vector2 pivot = troop.transform.position;
        StepHeading(troop.PivotVelocity, deltaTime);
        StepToward(Target(troop, pivot), pivot, deltaTime);
    }

    // The formation's outline this frame, before easing.
    TroopFootprint Target(Troop troop, Vector2 pivot)
    {
        TroopTuning tuning = TroopTuning.Active;
        int living = Mathf.Max(1, troop.LivingSoldierCount);
        switch (troop.FormationMode)
        {
            case FormationMode.Rout:
                return Blob(pivot, troop.Radius * tuning.RoutScatterRadiusScale, troop.Facing);
            case FormationMode.Line:
            case FormationMode.Chain:
                TroopFormationData formation = troop.Formation;
                if (formation != null) return Line(formation.Start, formation.End, formation.Facing, living);
                break;
            case FormationMode.Column:
                if (_marching) return Column(pivot, _heading, troop.Radius, troop.Frontage, living);
                break;
        }
        return Blob(pivot, troop.Radius * tuning.BlobRadiusScale, troop.Facing);
    }

    // --- Formation outlines (pure) ---

    // The battle line: a box from line point to line point, as deep as the line's ranks.
    public static TroopFootprint Line(Vector2 start, Vector2 end, Vector2 facing, int livingSoldiers)
    {
        float length = Vector2.Distance(start, end);
        return TroopFootprint.Rect(start, end, facing, LineGeometry.RankHalfWidth(length, livingSoldiers) + Padding);
    }

    // The marching column for a troop whose frontage comes from its soldier count (TroopFrontageRule, SU-117).
    public static TroopFootprint Column(Vector2 pivot, Vector2 heading, float troopRadius, int livingSoldiers) =>
        Column(pivot, heading, troopRadius, TroopFrontageRule.ForSoldierCount(livingSoldiers), livingSoldiers);

    // The marching column: a capsule around the soldier field's column core (SoldierFieldPlanner.Column), trailing the
    // pivot along -heading, with its long axis on the facing so that a line turning into a column (or back) morphs
    // without spinning. Its width is the column's lanes (LineGeometry.ColumnHalfWidth), not the troop radius (SU-120).
    public static TroopFootprint Column(Vector2 pivot, Vector2 heading, float troopRadius, int frontage, int livingSoldiers)
    {
        Vector2 forward = heading.sqrMagnitude > 1e-8f ? heading.normalized : Vector2.right;
        float trail = LineGeometry.ColumnTrail(troopRadius);
        float length = LineGeometry.ColumnLength(frontage, livingSoldiers);
        Vector2 center = pivot - forward * (trail + length * 0.5f);
        float laneHalfWidth = LineGeometry.ColumnHalfWidth(frontage, livingSoldiers) + Padding;
        return new TroopFootprint(FootprintKind.Capsule, center, forward, 0f, length * 0.5f, laneHalfWidth);
    }

    public static TroopFootprint Blob(Vector2 center, float radius, Vector2 facing) =>
        TroopFootprint.Circle(center, radius + Padding, facing);

    // --- Smoothing ---

    // Follows the march heading. The column holds for StopHoldSeconds after the troop stops.
    public void StepHeading(Vector2 pivotVelocity, float deltaTime)
    {
        float dt = Mathf.Max(0f, deltaTime);
        if (pivotVelocity.sqrMagnitude > SoldierFieldPlanner.MovingSpeedSquared)
        {
            Vector2 direction = pivotVelocity.normalized;
            if (!_marching)
            {
                _heading = direction;
                _marching = true;
            }
            else
            {
                float blend = 1f - Mathf.Exp(-dt / HeadingSmoothTime);
                float angle = Mathf.LerpAngle(AngleOf(_heading), AngleOf(direction), blend) * Mathf.Deg2Rad;
                _heading = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            }
            _stillTime = 0f;
            return;
        }
        _stillTime += dt;
        if (_stillTime >= StopHoldSeconds) _marching = false;
    }

    // Eases the outline toward the target (kept relative to the pivot). The first call snaps to it, and so does a call
    // with no time step (paused or deploying): nothing eases while time stands still, so a troop placed or re-ordered
    // then would otherwise keep an outline offset to where its formation used to be.
    public TroopFootprint StepToward(in TroopFootprint target, Vector2 pivot, float deltaTime)
    {
        _kind = target.Kind;
        if (!_seeded || deltaTime <= 0f)
        {
            _offset = target.Center - pivot;
            _facingAngle = AngleOf(target.Facing);
            _halfLength = target.HalfLength;
            _halfWidth = target.HalfWidth;
            _rounding = target.Rounding;
            _seeded = true;
            return At(pivot);
        }

        float blend = 1f - Mathf.Exp(-Mathf.Max(0f, deltaTime) / ShapeSmoothTime);
        _offset = Vector2.Lerp(_offset, target.Center - pivot, blend);
        _facingAngle = Mathf.LerpAngle(_facingAngle, AngleOf(target.Facing), blend);
        _halfLength = Mathf.Lerp(_halfLength, target.HalfLength, blend);
        _halfWidth = Mathf.Lerp(_halfWidth, target.HalfWidth, blend);
        _rounding = Mathf.Lerp(_rounding, target.Rounding, blend);
        return At(pivot);
    }

    static float AngleOf(Vector2 direction) => Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
}
