using System;
using UnityEngine;

namespace AchievementSystem
{
    // A side objective: the player holds this capture point when the battle ends (e.g. bonus gold for a reclaimed farm).
    // Set inline on the battle's AchievementController, since the capture point is a scene object.
    [Serializable]
    public class CapturedVictoryPoint : GlobalRequirement
    {
        [SerializeField] CapturePoint capturePoint;
        [SerializeField, HideInInspector] bool currentOwner;

        protected override bool CanTrack(AchievementContext context)
        {
            if (capturePoint != null) return true;
            Debug.LogWarning("Captured Victory Point achievement: no capture point set; the achievement is skipped.");
            return false;
        }

        protected override void OnBegin()
        {
            currentOwner = capturePoint.Controller == CapturePoint.PointController.Player;
            capturePoint.capturedBy += OnCaptured;
        }

        protected override void OnEnd()
        {
            if (capturePoint != null) capturePoint.capturedBy -= OnCaptured;
        }

        void OnCaptured(bool playerIsOwner) => currentOwner = playerIsOwner;

        public override int Level => currentOwner ? 1 : 0;
    }
}
