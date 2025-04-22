using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;
namespace AchievementSystem
{
    [CreateAssetMenu(menuName = "Achievements/Kills")]
    public class Kills : Achievments
    {
        [SerializeField, Range(1, 10)]
        int XPperKill;

        public override string Description
        {
            get
            {
                string description = "";
                foreach (var unit in KillsByEachUnit.Keys)
                {
                    description += unit.Stats.UnitName + ": " + KillsByEachUnit[unit] + ", ";
                }
                return description;
            }
        }

        public override bool Achieved()
        {
            return KillsByEachUnit.Count > 0;
        }

        public override void Initialise()
        {
            Notifications.MeleeDamage += RecordKills;
            KillsByEachUnit = new();
        }
        Dictionary<UnitTemplate, int> KillsByEachUnit;
        void RecordKills(UnitTemplate attacker, UnitTemplate victim, int kills)
        {
            if(attacker.army.controller != Army.Controller.Player)return;
            if(KillsByEachUnit.ContainsKey(attacker))
                KillsByEachUnit[attacker] += kills;
            else KillsByEachUnit.Add(attacker, kills);
        }
        public override void Reward()
        {
            foreach (var unit in KillsByEachUnit.Keys)
            {
                GiveExperience(unit.Stats, KillsByEachUnit[unit] * XPperKill);
            }
        }
    }
}