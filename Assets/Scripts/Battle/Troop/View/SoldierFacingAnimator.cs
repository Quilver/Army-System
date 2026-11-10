using UnityEngine;

// Turns a soldier's facing toward where it is heading (or toward the enemy line when engaged) at a limited turn rate.
public static class SoldierFacingAnimator
{
    const float FacingSpeedThreshold = 0.2f;
    const float TurnSpeed = 360f;
    const float EngagedFacingDeviation = 30f;

    // Updates soldier.Facing and returns the sprite's Z rotation in degrees.
    public static float Update(Troop troop, SoldierAgentState soldier, float deltaTime)
    {
        Vector2 velocity = troop.WorldVelocity(soldier);
        bool moving = velocity.magnitude > FacingSpeedThreshold;
        Vector2 desiredFacing = soldier.Facing;
        if (troop.HasThreatFacingFormation)
        {
            desiredFacing = troop.Formation.Facing.normalized;
            if (moving)
            {
                float deviation = Vector2.SignedAngle(desiredFacing, velocity.normalized);
                float clampedDeviation = Mathf.Clamp(deviation, -EngagedFacingDeviation, EngagedFacingDeviation);
                desiredFacing = Quaternion.AngleAxis(clampedDeviation, Vector3.forward) * desiredFacing;
            }
        }
        else if (moving)
        {
            desiredFacing = velocity.normalized;
        }

        Vector2 currentFacing = soldier.Facing.sqrMagnitude > 0.0001f ? soldier.Facing.normalized : (Vector2)troop.transform.right;
        if (deltaTime > 0f && desiredFacing.sqrMagnitude > 0.0001f)
        {
            float turn = Mathf.Clamp(
                Vector2.SignedAngle(currentFacing, desiredFacing),
                -TurnSpeed * deltaTime,
                TurnSpeed * deltaTime);
            currentFacing = Quaternion.AngleAxis(turn, Vector3.forward) * currentFacing;
            soldier.Facing = currentFacing.normalized;
        }

        return Mathf.Atan2(soldier.Facing.y, soldier.Facing.x) * Mathf.Rad2Deg - 90f;
    }
}