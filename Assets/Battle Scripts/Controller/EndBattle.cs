using Campaign;
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
        protected virtual void Init()
        {
            Notifications.InitEvents();
            Notifications.ArmyDestroyed += GameOver;
            BattleReport.Reset();
            BattleReport.timeTaken = Time.time;
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
        void KillCounter(UnitInterface attacker, UnitInterface defender, int damage)
        {
            if(Battle.Instance.player == Battle.Instance.unitArmy[attacker])
                BattleReport.kills += damage;
            else
                BattleReport.deaths+= damage;
        }
        void Victory()
        {
            BattleReport.timeTaken = Time.time - BattleReport.timeTaken;
            SceneManager.LoadScene(2);
        }
    }
}