using Campaign;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace EndGameUI
{
    public class UnitManager : MonoBehaviour
    {
        [SerializeField]
        GameObject unitCard;
        [SerializeField]
        List<PCWrapper> units;
        void Start()
        {
            InitUnits(Campaign.CampaignDataManager.Data.Characters);
            /*
            if (BattleReport.statWrappers == null)
                InitUnits(units);
            else
                InitUnits(BattleReport.statWrappers);
            */
        }
        void InitUnits(List<PCWrapper> units)
        {
            this.units = units;
            foreach (var unit in units)
            {
                var card = GameObject.Instantiate(unitCard, transform);
                card.GetComponent<UnitCard>().Set(unit);
            }
        }
    }
}