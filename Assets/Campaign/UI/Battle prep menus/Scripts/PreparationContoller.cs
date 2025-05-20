using Campaign;
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
        List<PreparationData> preparationData;
        public static PreparationData Data
        {
            get; private set;
        }
        private void Awake()
        {
            Data = preparationData[CampaignDataManager.Data.CurrentLevel-1];
            BattleReport.DeployedCharacters = new();
            _currentSelected = briefingTab;
        }
        public static event System.Action<ITab> OpenedTab;
        [SerializeField]
        ITab _currentSelected;
        public ITab SelectedTab
        {
            get => _currentSelected;
            protected set
            {
                _currentSelected.Select(deSelectColor, false);
                _currentSelected = value;
                _currentSelected.Select(selectColor, true);
                OpenedTab?.Invoke(_currentSelected);
            }
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
        public void SwapToBriefing() => SelectedTab = briefingTab;
        public void SwapToUnitSelection() => SelectedTab = unitSelectionTab;
        public void SwapToBuys() => SelectedTab = buyTab;
        public void SwapToSells() => SelectedTab = SellTab;


    }
}