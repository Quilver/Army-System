using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngineInternal;

namespace Campaign
{
    
    public class CampaignDataManager: MonoBehaviour 
    {
        public CampaignData data;
        CampaignDataManager _manager;
        public static CampaignDataManager instance { get; private set; }
        FileDataHandler SaverLoader;
        private void Awake()
        {
            if (instance != null)
            {
                Destroy(gameObject);
            }
            instance = this;
            SaverLoader= new FileDataHandler();
            DontDestroyOnLoad(gameObject);
        }
        public void NewGame()
        {
            Debug.Log("New game");
            SceneManager.LoadScene(1);
        }
        public void LoadGame()
        {
            data = SaverLoader.Load();
            if (data == null)
            {
                Debug.Log("no data found. starting new game");
                NewGame();
            }
            else
            {
                SceneManager.LoadScene(1);
            }
        }
        public void SaveGame()
        {
            SaverLoader.Save(data);
        }
        private void OnApplicationQuit()
        {
            SaveGame();
        }
    }
}
