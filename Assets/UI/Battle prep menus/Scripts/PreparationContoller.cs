using BattleFlowControl;
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
        public static PreparationData Data
        {
            get; private set;
        }
        private void Awake()
        {
            Data = preparationData;
            BattleReport.statWrappers = new();
        }
        [SerializeField]
        BriefingTab briefingTab;
        [SerializeField]
        UnitSelectionTab unitSelectionTab;
        [SerializeField]
        SellTab SellTab;
        [SerializeField]
        BuyTab buyTab;
        [SerializeField]
        Color selectColor, deSelectColor;
        void Deselect()
        {
            briefingTab.Select(deSelectColor, false);
            unitSelectionTab.Select(deSelectColor, false);
            buyTab.Select(deSelectColor, false);
            SellTab.Select(deSelectColor, false);
        }
        public void SwapToBriefing()
        {
            Deselect();
            briefingTab.Select(selectColor, true);
        }
        public void SwapToUnitSelection()
        {
            Deselect();
            unitSelectionTab.Select(selectColor, true);
        }
        public void SwapToBuys()
        {
            Deselect();
            SellTab.Select(selectColor, true);
        }
        public void SwapToSells()
        {
            Deselect();
            buyTab.Select(selectColor, true);
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