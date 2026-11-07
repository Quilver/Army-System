using System.Collections.Generic;
using Campaign;
using UnityEngine;

namespace EndGameUI
{
    // Post-battle screen: one card per troop the player deployed in the battle (SU-98).
    //   From the campaign: the characters deployed in the tent (BattleReport.DeployedCharacters), which earn the XP.
    //   A level played on its own: the troops the deployment zone spawned (BattleReport.DeployedTroops, the level's
    //   defaults). A troop whose character is in the roster shows that roster character; any other shows its character
    //   without level or XP.
    public class UnitManager : MonoBehaviour
    {
        [SerializeField]
        GameObject unitCard;

        void Start()
        {
            if (!BattleReport.Exists) return;
            List<RosterCharacter> characters = BattleReport.DeployedCharacters;
            if (characters != null && characters.Count > 0) InitUnits(characters);
            else InitTroops(BattleReport.DeployedTroops);
        }

        void InitUnits(List<RosterCharacter> characters)
        {
            foreach (var character in characters)
            {
                if (character == null) continue;
                NewCard().Set(character);
            }
        }

        void InitTroops(List<DeployedTroop> troops)
        {
            if (troops == null) return;
            List<RosterCharacter> roster = CampaignDataManager.instance != null ? CampaignDataManager.Data.Roster : null;
            foreach (var troop in troops)
            {
                if (troop == null) continue;
                RosterCharacter character = InRoster(roster, troop.FirstCharacter);
                if (character != null) NewCard().Set(character);
                else NewCard().Set(troop.Characters);
            }
        }

        static RosterCharacter InRoster(List<RosterCharacter> roster, CharacterProfile profile)
        {
            if (roster == null || profile == null) return null;
            foreach (var character in roster)
                if (character != null && character.character == profile) return character;
            return null;
        }

        UnitCard NewCard() => Instantiate(unitCard, transform).GetComponent<UnitCard>();
    }
}
