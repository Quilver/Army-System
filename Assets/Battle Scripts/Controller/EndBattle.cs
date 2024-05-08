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
        
        void Victory()
        {
            SceneManager.LoadScene(2);
        }
    }
}