using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EndGameUI
{
    public class EndGameFlow : MonoBehaviour
    {
        [SerializeField]
        TextMeshProUGUI time, kills, deaths;
        // Start is called before the first frame update
        void Start()
        {
            time.text = GetTime(BattleReport.timeTaken);
            kills.text= BattleReport.kills.ToString();
            deaths.text= BattleReport.deaths.ToString();
            Cursor.visible = true;
        }
        string GetTime(float timer)
        {
            int minutes = Mathf.FloorToInt(timer / 60F);
            int seconds = Mathf.FloorToInt(timer - minutes * 60);
            return string.Format("{0:00}:{1:00}", minutes, seconds);
        }

    }
}