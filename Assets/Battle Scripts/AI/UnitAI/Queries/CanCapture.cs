using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AISystem.Query
{
    [System.Serializable]
    public class CanCapture : IQuery
    {
        public override bool Query()
        {
            if(map.capturePoints.Count == 0) return false;
            foreach(var point in map.capturePoints)
                if(point.Controller != CapturePoint.PointController.Enemy)return true;
            return false;
        }
    }
}