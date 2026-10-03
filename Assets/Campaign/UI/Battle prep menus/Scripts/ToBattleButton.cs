using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ToBattleButton : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI Text;
    [SerializeField] Image image;
    [SerializeField] Button button;
    [SerializeField] Color ValidColour, InvalidColour;
    private void Update()
    {
        if (ValidSetup())
        {
            button.enabled = true;
            Text.text = "Start Battle";
            Text.color = ValidColour;
        }
        else
        {
            button.enabled = false;
            Text.color = InvalidColour;
            if (Campaign.CampaignDataManager.Data.Money == 0)
                Text.text = "Needs Money, Get A Loan";
            else if(BattleReport.DeployedCharacters.Count == 0)
                Text.text = "Select Units For Battle";
            else 
                Text.text = "Need At Least 2 Units For Battle";
        }
    }
    bool ValidSetup()
    {
        return BattleReport.DeployedCharacters.Count > 1;
    }
    public void NextLevel()

    {
        int endOfNonLevelSceneIndexes = 2;
        SceneManager.LoadScene(Campaign.CampaignDataManager.Data.CurrentLevel + endOfNonLevelSceneIndexes);
    }
}
