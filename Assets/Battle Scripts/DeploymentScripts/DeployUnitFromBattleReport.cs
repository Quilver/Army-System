using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;
namespace Deployment
{
    public class DeployUnitFromBattleReport : MonoBehaviour
    {
        [SerializeField]
        Army armyData;
        Vector3 Range;
        [SerializeField, Range(3, 10)] float unitSpacing = 6;
        private void Awake()
        {
            Range = transform.lossyScale/2;
            EvenDeployment();
        }
        void EvenDeployment()
        {
            var characters = BattleReport.DeployedCharacters;
            int files = (characters.Count * unitSpacing > Range.x) ? characters.Count  : (int)(Range.x / unitSpacing);
            int rows = characters.Count / files;
            for (int i = 0; i < characters.Count; i++)
            {
                var xPos = (i % files * unitSpacing - Range.x/2) * transform.right;
                var yPos = (Range.y/2 - i / files * unitSpacing) * transform.up;
                Vector3 position = transform.position + xPos + yPos;
                DeployUnit(characters[i], position);
            }

        }
        void RandomDeployment()
        {
            var characters = BattleReport.DeployedCharacters;
            foreach (var character in characters)
                DeployUnit(character, transform.position + new Vector3(Random.Range(-Range.x, Range.x), Random.Range(-Range.y, Range.y)));
        }
        void DeployUnit(Campaign.PCWrapper character, Vector3 position)
        {
            var unit = Instantiate(character.statBase.UnitPrefab, position, Quaternion.identity);
            unit.gameObject.name = character.statBase.UnitName;
            unit.transform.parent=armyData.transform;
            unit.Stats =character.GetStatsForBattle();
            _SetRangedCombat(unit, character);
            _SetWidth(unit, character);

            
        }
        void _SetRangedCombat(IUnit unit, Campaign.PCWrapper character)
        {
            var rangedWeapon = unit.transform.GetComponentInChildren<RangedWeapon>();
            if (character.statBase._rangedWeapon != null)
            {
                rangedWeapon.Setup(character.statBase);
                rangedWeapon.gameObject.SetActive(true);
            }
            else
            {
                rangedWeapon.gameObject.SetActive(false);
            }
        }
        void _SetWidth(IUnit unit, Campaign.PCWrapper character)
        {
            int width = 1;
            int models = character.statBase.ModelCount;
            if (models > 24) width = 8;
            else if (models > 18) width = 7;
            else if (models > 12) width = 6;
            else if (models > 10) width = 5;
            else if(models > 6) width = 4;
            else if(models > 3) width = 3;
            else if(models > 2) width = 2;
            else width = 1;
            unit.GetComponentInChildren<Formation.DeployFormationData>()._width = width;
        }
    }
}