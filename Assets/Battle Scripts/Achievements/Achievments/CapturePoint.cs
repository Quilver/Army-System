using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AchievementSystem
{
    [CreateAssetMenu(menuName = "Achievements/Captured points")]
    public class CapturePoint : Achievments
    {
        public override string Description => "Villages \'saved\'";
        [SerializeField, Range(1, 100)]
        int xpReward = 10;
        public override bool Achieved()
        {
            return capturePoints.Count > 0;
        }
        List<CaputurePoint> capturePoints;
        public override void Initialise()
        {
            capturePoints = new List<CaputurePoint>();
            CaputurePoint.CapturedBy += CaptureEvent;
        }
        void CaptureEvent(CaputurePoint capturePoint, Army.Controller controller)
        {
            if(capturePoint == null) return;
            if(controller == Army.Controller.Player && !capturePoints.Contains(capturePoint))
                capturePoints.Add(capturePoint);
            else if(controller == Army.Controller.Computer)
                capturePoints.Remove(capturePoint);
        }
        public override void Reward()
        {
            foreach (var character in BattleReport.statWrappers)
            {
                GiveExperience(character.statBase, xpReward);
            }
        }
    }
}

