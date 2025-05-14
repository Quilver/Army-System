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
            unit.transform.parent=armyData.transform;
            unit.Stats =character.GetStatsForBattle();
            if(character.statBase._projectile != null)
            {
                var rangedWeapon = unit.GetComponentInChildren<RangedWeapon>();
                rangedWeapon._projectile=character.statBase._projectile;
                rangedWeapon.gameObject.SetActive(true);
            }
        }
    }
}