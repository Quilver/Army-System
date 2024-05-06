using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BattlePrep
{
    public class PreparationContoller : MonoBehaviour
    {
        [SerializeField]
        PreparationData preparationData;
        #region Resources
        [SerializeField, Header("Resources")]
        TextMeshProUGUI Prestige;
        [SerializeField]
        TextMeshProUGUI Gold;
        #endregion
        #region Briefing Tab
        [SerializeField, Header("Briefing")]
        Image briefingButton;
        [SerializeField]
        RectTransform briefing;
        [SerializeField]
        Image briefingImage;
        public void SwapToBriefing()
        {
            briefingButton.color= new(1,1,1,0.5f);
            briefing.gameObject.SetActive(true);
            briefingImage.gameObject.SetActive(true);
            UnSwapToUnitSelection();
            UnSwapToBuys();
            UnSwapToSells();
        }
        void UnSwapToBriefing()
        {
            briefingButton.color = new(0.5f, 0.5f, 0.5f, 0.5f);
            briefing.gameObject.SetActive(false);
            briefingImage.gameObject.SetActive(false);
        }
        #endregion
        #region Unit selection Tab
        [SerializeField, Header("Unit Selection")]
        Image UnitSelectionButton;
        [SerializeField]
        RectTransform unitSelection;
        public void SwapToUnitSelection()
        {
            UnitSelectionButton.color = new(1, 1, 1, 0.5f);
            unitSelection.gameObject.SetActive(true);
            UnSwapToBriefing();
            UnSwapToBuys();
            UnSwapToSells();
        }
        void UnSwapToUnitSelection()
        {
            UnitSelectionButton.color = new(0.5f, 0.5f, 0.5f, 0.5f);
            unitSelection.gameObject.SetActive(false);
        }
        #endregion
        #region Buy Tab
        [SerializeField, Header("Buys")]
        Image BuyButton;
        [SerializeField]
        RectTransform buys;
        public void SwapToBuys()
        {
            BuyButton.color = new(1, 1, 1, 0.5f);
            buys.gameObject.SetActive(true);
            UnSwapToBriefing();
            UnSwapToUnitSelection();
            UnSwapToSells();
        }
        void UnSwapToBuys()
        {
            BuyButton.color = new(0.5f, 0.5f, 0.5f, 0.5f);
            buys.gameObject.SetActive(false);
        }
        #endregion
        #region Sell Tab
        [SerializeField, Header("Sells")]
        Image SellButton;
        [SerializeField]
        RectTransform sells;
        public void SwapToSells()
        {
            SellButton.color = new(1, 1, 1, 0.5f);
            sells.gameObject.SetActive(true);
            UnSwapToBriefing();
            UnSwapToUnitSelection();
            UnSwapToBuys();
        }
        void UnSwapToSells()
        {
            SellButton.color = new(0.5f, 0.5f, 0.5f, 0.5f);
            sells.gameObject.SetActive(false);
        }
        #endregion
        // Start is called before the first frame update
        void Start()
        {

        }
        [SerializeField]
        int CurrentLevel = 1;
        public void NextLevel()
        {
            int endOfNonLevelSceneIndexes = 2;
            SceneManager.LoadScene(CurrentLevel + endOfNonLevelSceneIndexes);
        }
        
        
    }
}