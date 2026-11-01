using System.Collections.Generic;
using Campaign;
using UnityEngine;

namespace EndGameUI
{
    // Post-battle screen: one card per roster character.
    public class UnitManager : MonoBehaviour
    {
        [SerializeField]
        GameObject unitCard;

        void Start()
        {
            InitUnits(CampaignDataManager.Data.Roster);
        }

        void InitUnits(List<RosterCharacter> characters)
        {
            if (characters == null) return;
            foreach (var character in characters)
            {
                var card = Instantiate(unitCard, transform);
                card.GetComponent<UnitCard>().Set(character);
            }
        }
    }
}
