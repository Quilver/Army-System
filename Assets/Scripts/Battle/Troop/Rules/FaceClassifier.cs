using System;
using UnityEngine;

// Which face of a defender an attacker is attacking (spec §4.8, Q-31, Q-32). Pure maths.
// The angle is measured from the defender's pivot to the attacker's nearest footprint point, relative to the defender's
// facing: within frontHalfAngle is the Front, beyond rearAngle the Rear, otherwise a flank (left when anticlockwise).
// Blob and Column troops use the same rule with their current heading as the facing.
public static class FaceClassifier
{
    public static TroopFace Classify(Vector2 defenderPivot, Vector2 defenderFacing, Vector2 attackerPoint,
        float frontHalfAngleDeg = 45f, float rearAngleDeg = 135f)
    {
        Vector2 toAttacker = attackerPoint - defenderPivot;
        if (toAttacker.sqrMagnitude < 1e-8f || defenderFacing.sqrMagnitude < 1e-8f) return TroopFace.Front;
        float angle = Vector2.SignedAngle(defenderFacing, toAttacker);
        float magnitude = Math.Abs(angle);
        // A small tolerance keeps exact boundary values (45, 135) on the Front and flank sides.
        if (magnitude <= frontHalfAngleDeg + 1e-3f) return TroopFace.Front;
        if (magnitude > rearAngleDeg + 1e-3f) return TroopFace.Rear;
        return angle > 0f ? TroopFace.LeftFlank : TroopFace.RightFlank;
    }

    public static TroopFace Classify(in TroopFootprint defender, Vector2 defenderPivot, in TroopFootprint attacker,
        float frontHalfAngleDeg = 45f, float rearAngleDeg = 135f) =>
        Classify(defenderPivot, defender.Facing, attacker.ClosestPoint(defenderPivot), frontHalfAngleDeg, rearAngleDeg);
}
