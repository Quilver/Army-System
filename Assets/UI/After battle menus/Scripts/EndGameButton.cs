using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EndGameUI
{
    public class EndGameButton : MonoBehaviour
    {
        public void Click()
        {
            Campaign.CampaignDataManager.instance.data.CurrentLevel++;
            if (SceneManager.sceneCountInBuildSettings + 3 > Campaign.CampaignDataManager.instance.data.CurrentLevel)
                SceneManager.LoadScene(1);
            else
                Debug.Log("Game over");
        }
    }
}