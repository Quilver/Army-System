using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AchievementSystem
{
    [Serializable]
    public class CapturedVictoryPoint : RequirementGlobal
    {
        [SerializeField]
        CapturePoint capturePoint;
        [SerializeField] bool currentOwner;
        public override void Setup()
        {
            if (capturePoint == null)
            {
                Debug.LogWarning("Capture point is not set");
                return;
            }
            currentOwner = capturePoint.Controller == CapturePoint.PointController.Player;
            capturePoint.capturedBy += GetOwner;
        }
        void GetOwner(bool playerIsOwner)
        {
            currentOwner = playerIsOwner;
        }
        public override int Succeeded()
        {
            return (currentOwner)? 1 : 0;
        }
    }
}