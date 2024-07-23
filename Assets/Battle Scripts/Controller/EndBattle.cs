using Campaign;
using StatSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BattleFlowControl
{
    public class EndBattle : MonoBehaviour
    {
        public float timeTaken;
        public int kills, deaths;
        [SerializeField]
        List<AchievementSystem.Achievments> Achievements;
        [SerializeField]
        List<Campaign.StatWrapper> statWrappers;
        // Start is called before the first frame update
        void Awake()
        {
            Init();
            
        }
        public Dictionary<UnitStats, StatWrapper> units;
        private void Start()
        {
            units = new();
            BattleReport.statWrappers = new();
            foreach (var unit in Battle.Instance.player.Units)
            {
                StatWrapper wrapper = new();
                wrapper.statBase = Instantiate(unit.UnitStats);
                //this.units.Add(unit.UnitStats, wrapper);
                BattleReport.statWrappers.Add(wrapper);
            }
        }
        protected virtual void Init()
        {
            //Notifications.ArmyDestroyed += GameOver;
            BattleReport.Reset();
            BattleReport.timeTaken = Time.time;
            BattleReport.Achievements = new();
            Notifications.MeleeDamage += KillCounter;
            Notifications.RangedDamage += KillCounter;

        }
        void GameOver(Army army)
        {
            if (Battle.Instance.player == army)
            {
                Debug.Log("Lost");
                SceneManager.LoadScene(0);
            }
            if(Battle.Instance.enemy1 == army)
            {
                Victory();
            }
        }
        void KillCounter(UnitBase attacker, UnitBase defender, int damage)
        {
            if(Battle.Instance.player == Battle.Instance.unitArmy[attacker])
                BattleReport.kills += damage;
            else
                BattleReport.deaths+= damage;
        }
        void Victory()
        {
            BattleReport.Achievements.Add(Achievements[0]);
            BattleReport.timeTaken = Time.time - BattleReport.timeTaken;
            SceneManager.LoadScene(2);
        }
    }
}