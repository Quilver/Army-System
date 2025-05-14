using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Deployment
{
    public class DeployUnitFromBattleReport : MonoBehaviour
    {
        [SerializeField]
        ArmyData armyData;
        Vector2 Range;
        private void Awake()
        {
            Range = transform.lossyScale/2;
            var characters = BattleReport.DeployedCharacters;
            foreach (var character in characters) 
                DeployUnit(character);
        }
        void DeployUnit(Campaign.PCWrapper character)
        {
            Vector3 position = transform.position + new Vector3(Random.Range(-Range.x, Range.x), Random.Range(-Range.y, Range.y));
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
            if (character.statBase._projectile != null)
            {
                rangedWeapon._projectile = character.statBase._projectile;
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
            if (models > 12) width = 8;
            else if(models > 6) width = 4;
            else if(models > 3) width = 3;
            else if(models > 2) width = 2;
            else width = 1;
            unit.GetComponentInChildren<Formation.DeployFormationData>()._width = width;
        }
    }
}