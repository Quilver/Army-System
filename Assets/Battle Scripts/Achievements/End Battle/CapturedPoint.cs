using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AchievementSystem.EndBattle
{
    public class CapturedPoint : MonoBehaviour
    {
        [SerializeField]
        bool WinCondition, playerControlled;
        [SerializeField]
        CapturePoint _capturePoint;
        void Start()
        {
            if (_capturePoint == null) _capturePoint = GetComponent<CapturePoint>();
            if (_capturePoint == null) Debug.LogError($"{gameObject.name} has not been assigned Capture point");
            _capturePoint.capturedBy += Captured;
        }
        void Captured(bool playerCaptured)
        {
            if (playerControlled == playerCaptured)
                Battle.Instance.EndBattle(WinCondition);
        }
    }
}